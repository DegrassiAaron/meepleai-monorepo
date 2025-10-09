using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Services;

/// <summary>
/// Client for PDF Parser sidecar service
/// Integrates Tabula-py and Camelot for advanced table extraction
/// </summary>
public interface IPdfParserClient
{
    Task<PdfParserResult> ExtractTablesAsync(string filePath, bool useCamelot = false, CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public class PdfParserClient : IPdfParserClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfParserClient> _logger;
    private readonly string _baseUrl;

    public PdfParserClient(HttpClient httpClient, ILogger<PdfParserClient> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["PDF_PARSER_URL"] ?? "http://pdf-parser:8000";

        _httpClient.BaseAddress = new Uri(_baseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // PDF processing can take time
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PDF Parser service is not available at {BaseUrl}", _baseUrl);
            return false;
        }
    }

    public async Task<PdfParserResult> ExtractTablesAsync(
        string filePath,
        bool useCamelot = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return PdfParserResult.CreateFailure("File path is required");
        }

        if (!File.Exists(filePath))
        {
            return PdfParserResult.CreateFailure($"File not found: {filePath}");
        }

        try
        {
            using var content = new MultipartFormDataContent();

            // Add PDF file
            var fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(streamContent, "file", Path.GetFileName(filePath));

            // Add use_camelot parameter
            content.Add(new StringContent(useCamelot.ToString().ToLowerInvariant()), "use_camelot");

            _logger.LogInformation(
                "Sending PDF to parser service: {FileName}, UseCamelot: {UseCamelot}",
                Path.GetFileName(filePath),
                useCamelot);

            var response = await _httpClient.PostAsync("/extract-tables", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "PDF Parser service returned error: {StatusCode}, {Content}",
                    response.StatusCode,
                    errorContent);
                return PdfParserResult.CreateFailure($"Service error: {response.StatusCode}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PdfParserResponse>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                return PdfParserResult.CreateFailure("Failed to parse response from PDF Parser service");
            }

            if (!result.Success)
            {
                _logger.LogWarning(
                    "PDF Parser service failed: {ErrorMessage}",
                    result.ErrorMessage);
                return PdfParserResult.CreateFailure(result.ErrorMessage ?? "Unknown error");
            }

            _logger.LogInformation(
                "PDF Parser extracted {TableCount} tables and {RuleCount} atomic rules using {Method}",
                result.Tables.Count,
                result.AtomicRules.Count,
                result.ExtractionMethod);

            return PdfParserResult.CreateSuccess(
                result.Tables,
                result.AtomicRules,
                result.ExtractionMethod);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling PDF Parser service at {BaseUrl}", _baseUrl);
            return PdfParserResult.CreateFailure($"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "PDF Parser service request timed out");
            return PdfParserResult.CreateFailure("Request timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling PDF Parser service");
            return PdfParserResult.CreateFailure($"Unexpected error: {ex.Message}");
        }
    }
}

/// <summary>
/// Response from PDF Parser service
/// </summary>
public class PdfParserResponse
{
    public bool Success { get; set; }
    public List<PdfParserTable> Tables { get; set; } = new();
    public List<string> AtomicRules { get; set; } = new();
    public string ExtractionMethod { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Table structure from PDF Parser service
/// </summary>
public class PdfParserTable
{
    public int PageNumber { get; set; }
    public List<string> Headers { get; set; } = new();
    public List<List<object>> Rows { get; set; } = new();
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public double? Accuracy { get; set; }
}

/// <summary>
/// Result of PDF Parser extraction
/// </summary>
public class PdfParserResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public List<PdfParserTable> Tables { get; init; } = new();
    public List<string> AtomicRules { get; init; } = new();
    public string ExtractionMethod { get; init; } = string.Empty;

    public static PdfParserResult CreateSuccess(
        List<PdfParserTable> tables,
        List<string> atomicRules,
        string extractionMethod) =>
        new()
        {
            Success = true,
            Tables = tables,
            AtomicRules = atomicRules,
            ExtractionMethod = extractionMethod
        };

    public static PdfParserResult CreateFailure(string errorMessage) =>
        new()
        {
            Success = false,
            ErrorMessage = errorMessage
        };
}
