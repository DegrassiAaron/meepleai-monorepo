# PDF Parser Sidecar Service

Python FastAPI service for advanced PDF table extraction using Tabula-py and Camelot.

## Features

- **Tabula-py**: Stream-mode extraction for text-based tables
- **Camelot**: Lattice-mode extraction for bordered tables
- **Atomic Rules**: Automatic conversion of table rows to structured rules
- **REST API**: Simple HTTP interface for integration with .NET API

## API Endpoints

### `GET /health`
Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "service": "pdf-parser"
}
```

### `POST /extract-tables`
Extract tables from PDF file.

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
      "column_count": 2
    }
  ],
  "atomic_rules": [
    "[Table on page 1] Column1: value1; Column2: value2"
  ],
  "extraction_method": "tabula",
  "error_message": null
}
```

## Running Locally

```bash
# Install dependencies
pip install -r requirements.txt

# Run the service
python -m app.main
```

The service will be available at `http://localhost:8000`.

## Docker

```bash
# Build image
docker build -t pdf-parser .

# Run container
docker run -p 8000:8000 pdf-parser
```

## Integration with .NET API

The .NET API can call this service via HTTP to extract tables from PDFs:

```csharp
var client = new HttpClient();
var content = new MultipartFormDataContent();
content.Add(new StreamContent(pdfStream), "file", "document.pdf");

var response = await client.PostAsync("http://pdf-parser:8000/extract-tables", content);
var result = await response.Content.ReadFromJsonAsync<TableExtractionResult>();
```

## Extraction Methods

### Tabula (Default)
- Best for: Stream-mode tables (text-based, no borders)
- Use when: Tables are formatted with whitespace/alignment

### Camelot
- Best for: Lattice-mode tables (bordered tables)
- Use when: Tables have visible borders and grid lines
- Set `use_camelot=true` in request

## Dependencies

- **Java**: Required for Tabula-py (PDF processing)
- **Ghostscript**: Required for Camelot (PDF rendering)
- **OpenCV**: Required for Camelot (image processing)
