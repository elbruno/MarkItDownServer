import logging
import os
import sys
import tempfile
from datetime import datetime
from pathlib import Path

from fastapi import FastAPI, UploadFile, File, HTTPException, Request
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from markitdown import MarkItDown

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[logging.StreamHandler(sys.stdout)]
)
logger = logging.getLogger(__name__)

# FastAPI app with metadata
app = FastAPI(
    title="MarkItDown Server",
    description="API for converting various document formats to Markdown",
    version="1.0.0",
    contact={
        "name": "El Bruno",
        "url": "https://github.com/elbruno/MarkItDownServer",
    },
    license_info={
        "name": "MIT",
        "url": "https://github.com/elbruno/MarkItDownServer/blob/main/LICENSE",
    },
)

# Add CORS middleware
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure appropriately for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# Security headers middleware
@app.middleware("http")
async def add_security_headers(request: Request, call_next):
    response = await call_next(request)
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "DENY"
    response.headers["X-XSS-Protection"] = "1; mode=block"
    return response

# Configuration
ALLOWED_EXTENSIONS = {'doc', 'docx', 'ppt', 'pptx', 'pdf', 'xls', 'xlsx', 'odt', 'ods', 'odp', 'txt'}
MAX_FILE_SIZE = 50 * 1024 * 1024  # 50MB

# Response models
class MarkdownResponse(BaseModel):
    markdown: str

class ErrorResponse(BaseModel):
    error: str

class HealthResponse(BaseModel):
    status: str
    timestamp: str
    service: str
    version: str

def allowed_file(filename: str | None) -> bool:
    """Check if the uploaded file has an allowed extension.
    
    Args:
        filename: Name of the file to check
        
    Returns:
        True if file extension is allowed, False otherwise
    """
    if not filename:
        return False
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

def convert_to_md(filepath: str) -> str:
    """Convert a file to Markdown format.
    
    Args:
        filepath: Path to the file to convert
        
    Returns:
        Markdown content as string
    """
    logger.info(f"Converting file: {filepath}")
    markitdown = MarkItDown()
    result = markitdown.convert(filepath)
    logger.info(f"Conversion result: {result.text_content[:100]}")
    return result.text_content

@app.get("/", summary="Root endpoint", description="Returns service information")
def read_root():
    """Get service information and available endpoints."""
    return {
        "service": "MarkItDown Server",
        "description": "API for converting documents to Markdown",
        "version": "1.0.0",
        "endpoints": {
            "health": "/health",
            "docs": "/docs",
            "process": "/process_file"
        }
    }

@app.get(
    "/health",
    response_model=HealthResponse,
    summary="Health check endpoint",
    description="Returns the health status of the service"
)
def health_check():
    """Get service health status."""
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "service": "MarkItDown Server",
        "version": "1.0.0"
    }

@app.post(
    '/process_file',
    response_model=MarkdownResponse,
    responses={
        400: {"model": ErrorResponse, "description": "Invalid file type or file too large"},
        413: {"model": ErrorResponse, "description": "File too large"},
        500: {"model": ErrorResponse, "description": "Internal server error"},
    },
    summary="Convert document to Markdown",
    description="Upload a document file and receive its content in Markdown format"
)
async def process_file(
    file: UploadFile = File(..., description="Document file to convert to Markdown")
) -> MarkdownResponse:
    """Process an uploaded file and convert it to Markdown."""
    temp_file_path = None
    
    # Validate filename
    if not file.filename:
        return JSONResponse(
            content={'error': 'Filename is required'}, 
            status_code=400
        )
    
    if not allowed_file(file.filename):
        return JSONResponse(
            content={'error': f'File type not allowed. Allowed types: {", ".join(ALLOWED_EXTENSIONS)}'}, 
            status_code=400
        )
    
    try:
        file_content = await file.read()
        
        # Validate file size
        if len(file_content) > MAX_FILE_SIZE:
            return JSONResponse(
                content={'error': f'File too large. Maximum size: {MAX_FILE_SIZE / (1024*1024):.0f}MB'}, 
                status_code=413
            )
        
        # Validate file is not empty
        if len(file_content) == 0:
            return JSONResponse(
                content={'error': 'File is empty'}, 
                status_code=400
            )
        
        # Save the file to a temporary directory with proper extension
        with tempfile.NamedTemporaryFile(
            delete=False, 
            suffix=Path(file.filename).suffix
        ) as temp_file:
            temp_file.write(file_content)
            temp_file_path = temp_file.name
            logger.info(f"Temporary file path: {temp_file_path}")
        
        # Convert the file to markdown
        markdown_content = convert_to_md(temp_file_path)
        logger.info("File converted to markdown successfully")
        
        return JSONResponse(content={'markdown': markdown_content})
        
    except Exception as e:
        logger.error(f"An error occurred: {str(e)}")
        return JSONResponse(content={'error': str(e)}, status_code=500)
        
    finally:
        # Ensure the temporary file is deleted
        if temp_file_path and os.path.exists(temp_file_path):
            os.remove(temp_file_path)
            logger.info(f"Temporary file deleted: {temp_file_path}")

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8490)     