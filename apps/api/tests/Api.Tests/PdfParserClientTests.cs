using Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Api.Tests;

public class PdfParserClientTests
{
    private readonly Mock<ILogger<PdfParserClient>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public PdfParserClientTests()
    {
        _loggerMock = new Mock<ILogger<PdfParserClient>>();
        _configurationMock = new Mock<IConfiguration>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        // Setup default configuration
        _configurationMock.Setup(c => c["PDF_PARSER_URL"]).Returns("http://pdf-parser:8000");
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceResponds_ReturnsTrue()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().EndsWith("/health")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"status\":\"healthy\"}")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceUnavailable_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.IsAvailableAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ExtractTablesAsync_WithValidPdf_ReturnsSuccess()
    {
        // Arrange
        var testPdfPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(testPdfPath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // PDF header

        var responseData = new PdfParserResponse
        {
            Success = true,
            Tables = new List<PdfParserTable>
            {
                new PdfParserTable
                {
                    PageNumber = 1,
                    Headers = new List<string> { "Column1", "Column2" },
                    Rows = new List<List<object>>
                    {
                        new List<object> { "Value1", "Value2" }
                    },
                    RowCount = 1,
                    ColumnCount = 2
                }
            },
            AtomicRules = new List<string>
            {
                "[Table on page 1] Column1: Value1; Column2: Value2"
            },
            ExtractionMethod = "tabula"
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/extract-tables")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(jsonResponse)
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        try
        {
            // Act
            var result = await client.ExtractTablesAsync(testPdfPath);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Tables);
            Assert.Single(result.AtomicRules);
            Assert.Equal("tabula", result.ExtractionMethod);
            Assert.Equal("[Table on page 1] Column1: Value1; Column2: Value2", result.AtomicRules[0]);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testPdfPath))
            {
                File.Delete(testPdfPath);
            }
        }
    }

    [Fact]
    public async Task ExtractTablesAsync_WithInvalidPath_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.ExtractTablesAsync("");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File path is required", result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractTablesAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        // Act
        var result = await client.ExtractTablesAsync("/path/to/nonexistent.pdf");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.ErrorMessage!);
    }

    [Fact]
    public async Task ExtractTablesAsync_WhenServiceReturnsError_ReturnsFailure()
    {
        // Arrange
        var testPdfPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(testPdfPath, new byte[] { 0x25, 0x50, 0x44, 0x46 });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Internal server error")
            });

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        var client = new PdfParserClient(httpClient, _loggerMock.Object, _configurationMock.Object);

        try
        {
            // Act
            var result = await client.ExtractTablesAsync(testPdfPath);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Service error", result.ErrorMessage!);
        }
        finally
        {
            if (File.Exists(testPdfPath))
            {
                File.Delete(testPdfPath);
            }
        }
    }
}
