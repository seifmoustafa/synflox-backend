using Application.DTOs;

namespace Application.Services
{
    /// <summary>
    /// Service for downloading files with support for chunked downloads and regular downloads
    /// </summary>
    public interface IDownloadService
    {
        /// <summary>
        /// Initiates a chunked download session and returns download info
        /// </summary>
        Task<DownloadInfo> InitiateAsync(string fileRequestPath, string? schemeName = null);

        /// <summary>
        /// Gets download status for a download session
        /// </summary>
        Task<DownloadStatus> GetStatusAsync(string downloadId);

        /// <summary>
        /// Downloads a specific chunk of a file
        /// </summary>
        Task<Stream> DownloadChunkAsync(string downloadId, int chunkIndex, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a file directly (non-chunked) with optional range support
        /// </summary>
        Task<FileDownloadResult> DownloadFileAsync(string fileRequestPath, string? schemeName = null, long? rangeStart = null, long? rangeEnd = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information without downloading
        /// </summary>
        Task<FileInfoDto> GetFileInfoAsync(string fileRequestPath, string? schemeName = null);
    }
}

