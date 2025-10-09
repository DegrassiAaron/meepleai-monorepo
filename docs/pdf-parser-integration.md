# PDF Parser Integration (ISSUE-256 / PDF-04)

## Overview

This document describes the integration of Tabula-py and Camelot PDF table parsers via a Python sidecar service to improve table extraction and normalization into RuleSpec format.

## Architecture

### Components

1. **Python Sidecar Service** (`apps/pdf-parser/`)
   - FastAPI-based HTTP service
   - Integrates Tabula-py and Camelot libraries
   - Exposes REST API for table extraction

2. **.NET Client** (`PdfParserClient.cs`)
   - HTTP client for communicating with sidecar
   - Handles multipart form uploads
   - Provides typed response models

3. **Integration Layer** (`PdfTableExtractionService.cs`)
   - Tries sidecar first for advanced extraction
   - Falls back to iText7 if sidecar unavailable
   - Seamless degradation strategy

### Data Flow

```
PDF Upload
    ↓
PdfTableExtractionService
    ↓
[Try] PdfParserClient → Python Sidecar (Tabula/Camelot)
    ↓ (if available)
Tables + Atomic Rules
    ↓ (if unavailable)
[Fallback] iText7 Extraction
    ↓
PdfStructuredExtractionResult
    ↓
RuleSpecService (atomic rules → RuleSpec)
```

## Python Sidecar Service

### Endpoints

#### `GET /health`
Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "service": "pdf-parser"
}
```

#### `POST /extract-tables`
Extract tables from PDF.

**Request:**
- `file`: PDF file (multipart/form-data)
- `use_camelot`: Boolean (optional, default: false)

**Response:**
```json
{
  "success": true,
  "tables": [
    {
      "page_number": 1,
      "headers": ["Column1", "Column2"],
      "rows": [["value1", "value2"]],
      "row_count": 1,
      "column_count": 2,
      "accuracy": 0.95
    }
  ],
  "atomic_rules": [
    "[Table on page 1] Column1: value1; Column2: value2"
  ],
  "extraction_method": "tabula"
}
```

### Extraction Methods

#### Tabula (Default)
- **Best for:** Stream-mode tables (text-based, no borders)
- **Use when:** Tables formatted with whitespace/alignment
- **Library:** tabula-py (Python wrapper for Tabula Java)

#### Camelot
- **Best for:** Lattice-mode tables (bordered tables)
- **Use when:** Tables have visible borders and grid lines
- **Library:** camelot-py with OpenCV
- **Usage:** Set `use_camelot=true` in request

### Dependencies

- **Java**: Required by Tabula (PDF processing)
- **Ghostscript**: Required by Camelot (PDF rendering)
- **OpenCV**: Required by Camelot (image processing)

## .NET Integration

### PdfParserClient

HTTP client for communicating with the Python sidecar.

```csharp
public interface IPdfParserClient
{
    Task<PdfParserResult> ExtractTablesAsync(
        string filePath,
        bool useCamelot = false,
        CancellationToken cancellationToken = default);

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
```

**Configuration:**
- Environment variable: `PDF_PARSER_URL`
- Default: `http://pdf-parser:8000`
- Timeout: 5 minutes (PDF processing can be slow)

### PdfTableExtractionService

Orchestrates extraction with fallback strategy.

```csharp
public async Task<PdfStructuredExtractionResult> ExtractStructuredContentAsync(
    string filePath,
    CancellationToken ct = default)
{
    // 1. Check if sidecar is available
    if (await _pdfParserClient.IsAvailableAsync(ct))
    {
        // 2. Try extraction with sidecar
        var result = await _pdfParserClient.ExtractTablesAsync(filePath, useCamelot: false, ct);

        if (result.Success && result.AtomicRules.Count > 0)
        {
            return ConvertToInternalFormat(result);
        }
    }

    // 3. Fallback to iText7
    return await ExtractWithItext7(filePath, ct);
}
```

### Dependency Injection

Registered in `Program.cs`:

```csharp
// Configure HttpClient for PdfParserClient
builder.Services.AddHttpClient<IPdfParserClient, PdfParserClient>();
```

## Docker Compose Integration

The sidecar is deployed as a service in `docker-compose.yml`:

```yaml
pdf-parser:
  build:
    context: ../apps/pdf-parser
    dockerfile: ./Dockerfile
  restart: unless-stopped
  ports:
    - "8001:8000"
  healthcheck:
    test: ["CMD-SHELL", "curl --fail http://localhost:8000/health || exit 1"]
    interval: 30s
    timeout: 10s
    retries: 3
    start_period: 40s
  networks:
    - meepleai
```

The API service depends on the sidecar:

```yaml
api:
  environment:
    PDF_PARSER_URL: http://pdf-parser:8000
  depends_on:
    pdf-parser:
      condition: service_healthy
```

## Atomic Rules Format

Tables are converted to atomic rules with the format:

```
[Table on page {page_number}] {header1}: {value1}; {header2}: {value2}; ...
```

**Example:**

Given a table:

| Action      | Cost | Effect        |
|-------------|------|---------------|
| Move        | 1    | Move 1 space  |
| Attack      | 2    | Deal 2 damage |

Generates:

```
[Table on page 3] Action: Move; Cost: 1; Effect: Move 1 space
[Table on page 3] Action: Attack; Cost: 2; Effect: Deal 2 damage
```

## RuleSpec Integration

The TODO at `RuleSpecService.cs:60` has been resolved:

```csharp
/// <summary>
/// PDF Parser integration (ISSUE-256 / PDF-04)
///
/// Advanced table extraction is now integrated via the PDF Parser sidecar service:
/// - Primary: Tabula-py for stream-mode tables (text-based)
/// - Alternative: Camelot for lattice-mode tables (bordered)
/// - Fallback: iText7 for extraction when sidecar unavailable
///
/// The integration happens in PdfTableExtractionService.ExtractStructuredContentAsync(),
/// which automatically tries the sidecar first and falls back to iText7 if needed.
///
/// Table data is normalized into atomic rules format compatible with RuleSpec.
/// </summary>
```

## Testing

### Unit Tests

**PdfParserClientTests.cs:**
- Tests HTTP client behavior
- Mocks HTTP responses
- Validates error handling

**PdfTableExtractionServiceTests.cs:**
- Tests fallback behavior
- Validates iText7 extraction
- Mocks PdfParserClient as unavailable

### Running Tests

```bash
# Run all PDF parser tests
cd apps/api
dotnet test --filter "FullyQualifiedName~PdfParser"

# Run specific test class
dotnet test --filter "FullyQualifiedName~PdfParserClientTests"
```

## Deployment

### Local Development

```bash
# Start all services including pdf-parser
cd infra
docker compose up -d

# Verify pdf-parser is running
curl http://localhost:8001/health
```

### Building Sidecar

```bash
cd apps/pdf-parser
docker build -t pdf-parser .
docker run -p 8000:8000 pdf-parser
```

### Environment Variables

**API (.env.dev):**
```bash
PDF_PARSER_URL=http://pdf-parser:8000
```

## Monitoring

### Logs

**Check sidecar logs:**
```bash
docker compose logs -f pdf-parser
```

**Check API logs for extraction:**
```bash
docker compose logs -f api | grep "PDF Parser"
```

### Health Check

The sidecar includes a health check that Docker monitors:

```bash
docker compose ps pdf-parser
```

Expected output:
```
NAME          STATUS                   HEALTH
pdf-parser    Up 2 minutes (healthy)
```

## Performance

### Extraction Times (Approximate)

- **Tabula:** 2-10 seconds per PDF
- **Camelot:** 5-20 seconds per PDF (more CPU-intensive)
- **iText7 fallback:** 1-5 seconds per PDF

### Timeout Configuration

The .NET client has a 5-minute timeout:

```csharp
_httpClient.Timeout = TimeSpan.FromMinutes(5);
```

Adjust if needed for very large PDFs.

## Troubleshooting

### Sidecar Not Available

**Symptom:** API falls back to iText7 every time

**Diagnosis:**
```bash
# Check if sidecar is running
docker compose ps pdf-parser

# Check sidecar logs
docker compose logs pdf-parser

# Test health endpoint
curl http://localhost:8001/health
```

**Solutions:**
- Rebuild sidecar: `docker compose build pdf-parser`
- Check network connectivity between containers
- Verify `PDF_PARSER_URL` environment variable

### Java Not Found (Tabula)

**Symptom:** Sidecar returns errors about Java

**Solution:**
The Dockerfile installs Java (`default-jre`). If missing:
```dockerfile
RUN apt-get update && apt-get install -y default-jre
```

### Ghostscript Issues (Camelot)

**Symptom:** Camelot extraction fails

**Solution:**
The Dockerfile installs Ghostscript. Verify:
```dockerfile
RUN apt-get update && apt-get install -y ghostscript
```

### Poor Extraction Quality

**Try alternative method:**
- If using Tabula (default), try Camelot: set `use_camelot=true`
- Camelot works better for bordered tables
- Tabula works better for text-aligned tables

### Performance Issues

**Optimization strategies:**
1. Use appropriate extraction method (Tabula vs Camelot)
2. Process PDFs asynchronously
3. Cache extraction results
4. Consider scaling sidecar horizontally for high load

## Future Enhancements

1. **Diagram Extraction**: Integrate OCR for flowcharts/diagrams
2. **Batch Processing**: Support multiple PDF uploads
3. **Caching**: Cache extraction results in Redis
4. **Model Selection**: Auto-detect best extraction method per page
5. **Progress Reporting**: WebSocket updates for long extractions
6. **Quality Metrics**: Return confidence scores with extracted data

## References

- **Tabula-py**: https://github.com/chezou/tabula-py
- **Camelot**: https://camelot-py.readthedocs.io/
- **iText7**: https://github.com/itext/itext7-dotnet
- **Issue #256**: PDF-04 - Integrate PDF Parser (Tabula/Camelot)
