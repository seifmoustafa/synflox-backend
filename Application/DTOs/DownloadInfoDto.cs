namespace Application.DTOs
{
    /// <summary>
    /// Information about a chunked download session
    /// </summary>
    public class DownloadInfo
    {
        public string DownloadId { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public long ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public string ContentType { get; set; } = null!;
    }

    /// <summary>
    /// Status of a chunked download
    /// </summary>
    public class DownloadStatus
    {
        public string DownloadId { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public long ChunkSize { get; set; }
        public int TotalChunks { get; set; }
        public int[] DownloadedChunks { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// File information DTO
    /// </summary>
    public class FileInfoDto
    {
        public string FileName { get; set; } = null!;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = null!;
        public DateTime LastModified { get; set; }
        public string FileUrl { get; set; } = null!;
    }

    /// <summary>
    /// Result of a file download
    /// </summary>
    public class FileDownloadResult
    {
        public Stream Stream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }
        public long? RangeStart { get; set; }
        public long? RangeEnd { get; set; }
        public bool IsRangeRequest { get; set; }
    }
}

