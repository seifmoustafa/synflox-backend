using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/downloads")]
public class DownloadsController : ControllerBase
{
    private readonly IDownloadService _downloadService;
    private readonly IFileService _fileService;

    public DownloadsController(IDownloadService downloadService, IFileService fileService)
    {
        _downloadService = downloadService;
        _fileService = fileService;
    }

    /// <summary>
    /// Initiates a chunked download session
    /// </summary>
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiateDownloadRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FileRequestPath))
            return BadRequest("FileRequestPath is required");

        try
        {
            var info = await _downloadService.InitiateAsync(request.FileRequestPath, request.Scheme);
            return Ok(info);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = "FILE_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INITIATE_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the status of a download session
    /// </summary>
    [HttpGet("{downloadId}/status")]
    public async Task<IActionResult> Status(string downloadId)
    {
        try
        {
            var status = await _downloadService.GetStatusAsync(downloadId);
            return Ok(status);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = "DOWNLOAD_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "STATUS_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a specific chunk
    /// </summary>
    [HttpGet("{downloadId}/chunk")]
    public async Task<IActionResult> DownloadChunk(string downloadId, [FromQuery] int index)
    {
        try
        {
            var stream = await _downloadService.DownloadChunkAsync(downloadId, index, HttpContext.RequestAborted);

            // Get download info for headers
            var status = await _downloadService.GetStatusAsync(downloadId);
            
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{status.FileName}\"";
            Response.Headers["Content-Type"] = "application/octet-stream";
            Response.Headers["X-Chunk-Index"] = index.ToString();
            Response.Headers["X-Total-Chunks"] = status.TotalChunks.ToString();

            return File(stream, "application/octet-stream", enableRangeProcessing: false);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = "DOWNLOAD_NOT_FOUND", message = ex.Message });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { error = "INVALID_CHUNK_INDEX", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "DOWNLOAD_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads a file directly (supports range requests for partial content).
    /// If deleteAfterDownload=true, the file will be deleted after successful download.
    /// </summary>
    [HttpGet("file")]
    public async Task<IActionResult> DownloadFile(
        [FromQuery] string fileRequestPath,
        [FromQuery] string? scheme = null,
        [FromQuery] bool deleteAfterDownload = false)
    {
        string? schemeToUse = scheme ?? "any";
        bool shouldDelete = deleteAfterDownload;
        
        try
        {
            // Support HTTP Range requests
            long? rangeStart = null;
            long? rangeEnd = null;

            var rangeHeader = Request.Headers["Range"].FirstOrDefault();
            if (!string.IsNullOrEmpty(rangeHeader))
            {
                var range = ParseRangeHeader(rangeHeader);
                if (range.HasValue)
                {
                    rangeStart = range.Value.Start;
                    rangeEnd = range.Value.End;
                }
            }

            var result = await _downloadService.DownloadFileAsync(
                fileRequestPath, 
                schemeToUse, 
                rangeStart, 
                rangeEnd, 
                HttpContext.RequestAborted);

            // Set response headers
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{WebUtility.UrlEncode(result.FileName)}\"";
            Response.Headers["Content-Type"] = result.ContentType;
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.Headers["Content-Length"] = result.IsRangeRequest 
                ? (result.RangeEnd!.Value - result.RangeStart!.Value + 1).ToString()
                : result.FileSize.ToString();

            // Set range response headers if this is a range request
            if (result.IsRangeRequest)
            {
                Response.StatusCode = 206; // Partial Content
                Response.Headers["Content-Range"] = $"bytes {result.RangeStart}-{result.RangeEnd}/{result.FileSize}";
            }

            // Register callback to delete file after response is sent (only if deleteAfterDownload=true)
            if (shouldDelete)
            {
                Response.OnCompleted(async () =>
                {
                    try
                    {
                        await _fileService.DeleteFileAsync(fileRequestPath, schemeToUse);
                    }
                    catch
                    {
                        // Silently fail - file might already be deleted or not exist
                    }
                });
            }

            return File(result.Stream, result.ContentType, result.FileName);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = "FILE_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "DOWNLOAD_ERROR", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets file information without downloading
    /// </summary>
    [HttpGet("info")]
    public async Task<IActionResult> GetFileInfo(
        [FromQuery] string fileRequestPath,
        [FromQuery] string? scheme = null)
    {
        try
        {
            var info = await _downloadService.GetFileInfoAsync(fileRequestPath, scheme);
            return Ok(info);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { error = "FILE_NOT_FOUND", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "INFO_ERROR", message = ex.Message });
        }
    }

    public class InitiateDownloadRequest
    {
        public string FileRequestPath { get; set; } = null!;
        public string? Scheme { get; set; }
    }

    private static (long Start, long End)? ParseRangeHeader(string rangeHeader)
    {
        // Format: "bytes=start-end" or "bytes=start-" or "bytes=-suffix"
        if (!rangeHeader.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase))
            return null;

        var range = rangeHeader.Substring(6); // Remove "bytes="
        var parts = range.Split('-');
        
        if (parts.Length != 2)
            return null;

        if (long.TryParse(parts[0], out var start))
        {
            if (long.TryParse(parts[1], out var end))
            {
                return (start, end);
            }
            else
            {
                // "bytes=start-" - from start to end of file
                return (start, long.MaxValue);
            }
        }
        else if (long.TryParse(parts[1], out var suffix))
        {
            // "bytes=-suffix" - last suffix bytes
            return (0, suffix);
        }

        return null;
    }
}

