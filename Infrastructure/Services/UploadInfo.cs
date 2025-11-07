namespace Infrastructure.Services
{
    public class UploadInfo
    {
        public required string Folder { get; init; }
        public required long ChunkSize { get; init; }
        public string? FileName { get; init; }
        public long? TotalBytes { get; init; }
        public required string SchemeName { get; init; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
