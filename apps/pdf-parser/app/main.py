"""
PDF Parser Sidecar Service
Provides advanced table extraction using Tabula-py and Camelot
"""
from fastapi import FastAPI, File, UploadFile, HTTPException
from fastapi.responses import JSONResponse
import tabula
import camelot
import tempfile
import os
from typing import List, Dict, Any
from pydantic import BaseModel
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(
    title="PDF Parser Service",
    description="Sidecar service for advanced PDF table extraction",
    version="1.0.0"
)


class TableExtractionResult(BaseModel):
    success: bool
    tables: List[Dict[str, Any]]
    atomic_rules: List[str]
    extraction_method: str
    error_message: str = None


@app.get("/health")
async def health_check():
    """Health check endpoint"""
    return {"status": "healthy", "service": "pdf-parser"}


@app.post("/extract-tables", response_model=TableExtractionResult)
async def extract_tables(
    file: UploadFile = File(...),
    use_camelot: bool = False
):
    """
    Extract tables from PDF using Tabula-py or Camelot

    Args:
        file: PDF file to process
        use_camelot: Use Camelot instead of Tabula (default: False)

    Returns:
        TableExtractionResult with extracted tables and atomic rules
    """
    if not file.filename.endswith('.pdf'):
        raise HTTPException(status_code=400, detail="Only PDF files are supported")

    temp_file_path = None

    try:
        # Save uploaded file to temporary location
        with tempfile.NamedTemporaryFile(delete=False, suffix='.pdf') as temp_file:
            temp_file_path = temp_file.name
            content = await file.read()
            temp_file.write(content)

        logger.info(f"Processing PDF: {file.filename} with {'Camelot' if use_camelot else 'Tabula'}")

        if use_camelot:
            result = extract_with_camelot(temp_file_path)
        else:
            result = extract_with_tabula(temp_file_path)

        logger.info(f"Extracted {len(result['tables'])} tables, {len(result['atomic_rules'])} rules")

        return TableExtractionResult(
            success=True,
            tables=result['tables'],
            atomic_rules=result['atomic_rules'],
            extraction_method=result['method']
        )

    except Exception as e:
        logger.error(f"Error processing PDF: {str(e)}", exc_info=True)
        return TableExtractionResult(
            success=False,
            tables=[],
            atomic_rules=[],
            extraction_method="none",
            error_message=str(e)
        )

    finally:
        # Clean up temporary file
        if temp_file_path and os.path.exists(temp_file_path):
            try:
                os.unlink(temp_file_path)
            except Exception as e:
                logger.warning(f"Failed to delete temp file: {e}")


def extract_with_tabula(file_path: str) -> Dict[str, Any]:
    """
    Extract tables using Tabula-py
    Tabula is better for stream-mode tables (text-based)
    """
    tables = []
    atomic_rules = []

    # Extract all tables from PDF
    dfs = tabula.read_pdf(
        file_path,
        pages='all',
        multiple_tables=True,
        lattice=False,  # Use stream mode for better detection
        stream=True
    )

    for page_idx, df in enumerate(dfs, start=1):
        if df.empty:
            continue

        # Convert DataFrame to structured table
        table_data = {
            'page_number': page_idx,
            'headers': df.columns.tolist(),
            'rows': df.values.tolist(),
            'row_count': len(df),
            'column_count': len(df.columns)
        }

        tables.append(table_data)

        # Generate atomic rules from table
        rules = convert_table_to_rules(table_data, page_idx)
        atomic_rules.extend(rules)

    return {
        'method': 'tabula',
        'tables': tables,
        'atomic_rules': atomic_rules
    }


def extract_with_camelot(file_path: str) -> Dict[str, Any]:
    """
    Extract tables using Camelot
    Camelot is better for lattice-mode tables (bordered tables)
    """
    tables = []
    atomic_rules = []

    # Try lattice mode first (for bordered tables)
    try:
        camelot_tables = camelot.read_pdf(file_path, pages='all', flavor='lattice')
    except Exception:
        # Fallback to stream mode
        camelot_tables = camelot.read_pdf(file_path, pages='all', flavor='stream')

    for idx, table in enumerate(camelot_tables):
        df = table.df
        page_num = table.page

        if df.empty:
            continue

        # Convert DataFrame to structured table
        headers = df.iloc[0].tolist() if len(df) > 0 else []
        rows = df.iloc[1:].values.tolist() if len(df) > 1 else []

        table_data = {
            'page_number': page_num,
            'headers': headers,
            'rows': rows,
            'row_count': len(rows),
            'column_count': len(headers),
            'accuracy': float(table.accuracy) if hasattr(table, 'accuracy') else None
        }

        tables.append(table_data)

        # Generate atomic rules from table
        rules = convert_table_to_rules(table_data, page_num)
        atomic_rules.extend(rules)

    return {
        'method': 'camelot',
        'tables': tables,
        'atomic_rules': atomic_rules
    }


def convert_table_to_rules(table: Dict[str, Any], page_number: int) -> List[str]:
    """
    Convert table rows to atomic rules
    Each row becomes a structured rule statement
    """
    rules = []
    headers = table.get('headers', [])
    rows = table.get('rows', [])

    if not headers or not rows:
        return rules

    for row in rows:
        rule_parts = []

        for i, cell_value in enumerate(row):
            if i < len(headers) and cell_value and str(cell_value).strip():
                header = str(headers[i]).strip()
                value = str(cell_value).strip()

                # Skip empty or null values
                if header and value and value.lower() not in ['nan', 'none', '']:
                    rule_parts.append(f"{header}: {value}")

        if rule_parts:
            rule = f"[Table on page {page_number}] {'; '.join(rule_parts)}"
            rules.append(rule)

    return rules


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)
