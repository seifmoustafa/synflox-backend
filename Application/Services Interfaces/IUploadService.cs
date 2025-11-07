using Application.DTOs;
using System.IO;

namespace Application.Services
{

    public interface IUploadService
    {
        Task<(string uploadId, long chunkSize)> InitiateAsync(string schemeName, string? fileName, long? totalBytes);
        Task<UploadStatus> UploadChunkAsync(string uploadId, int index, Stream data, long? chunkLength = null, CancellationToken cancellationToken = default);
        Task<UploadStatus> GetStatusAsync(string uploadId);
        Task<string> CompleteAsync(string uploadId, int totalChunks, string? fileName, string? expectedSha256, CancellationToken cancellationToken = default);
        Task<string> ConsumeAsync(string uploadId);
        Task AbortAsync(string uploadId);
        
        // Keep synchronous versions for backward compatibility
        (string uploadId, long chunkSize) Initiate(string schemeName, string? fileName, long? totalBytes);
        Task<UploadStatus> SaveChunkAsync(string uploadId, int index, Stream data, long? chunkLength = null, CancellationToken cancellationToken = default);
        UploadStatus GetStatus(string uploadId);
        Task CompleteUploadAsync(string uploadId, int totalChunks, string? fileName, string? expectedSha256, CancellationToken cancellationToken = default);
        string Consume(string uploadId);
        void Delete(string uploadId);
    }

    public record UploadStatus(int[] Parts, long Bytes, long ChunkSize);
}