using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController : ControllerBase
{
    private readonly IUploadService _uploadService;

    public UploadsController(IUploadService uploadService)
    {
        _uploadService = uploadService;
    }

    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiateUploadRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Scheme))
            return BadRequest();
        var (id, chunk) = await _uploadService.InitiateAsync(request.Scheme, request.FileName, request.TotalBytes);
        return Ok(new { uploadId = id, chunkSize = chunk });
    }

    [HttpPut("{uploadId}/chunk")]
    public async Task<IActionResult> UploadChunk(string uploadId, [FromQuery] int index)
    {
        var status = await _uploadService.UploadChunkAsync(uploadId, index, Request.Body, null, HttpContext.RequestAborted);
        return Ok(status);
    }

    [HttpGet("{uploadId}/status")]
    public async Task<IActionResult> Status(string uploadId)
    {
        var status = await _uploadService.GetStatusAsync(uploadId);
        return Ok(status);
    }

    [HttpPost("{uploadId}/complete")]
    public async Task<IActionResult> Complete(string uploadId, [FromBody] CompleteUploadRequest request)
    {
        if (request == null)
            return BadRequest();
        var url = await _uploadService.CompleteAsync(uploadId, request.TotalChunks, request.FileName, request.ExpectedSha256, HttpContext.RequestAborted);
        return Ok(new { fileUrl = url });
    }

    [HttpDelete("{uploadId}")]
    public async Task<IActionResult> Abort(string uploadId)
    {
        await _uploadService.AbortAsync(uploadId);
        return NoContent();
    }

    public class InitiateUploadRequest
    {
        public string Scheme { get; set; } = "Any";
        public string? FileName { get; set; }
        public long? TotalBytes { get; set; }
    }

    public class CompleteUploadRequest
    {
        public int TotalChunks { get; set; }
        public string? FileName { get; set; }
        public string? ExpectedSha256 { get; set; }
    }
}
