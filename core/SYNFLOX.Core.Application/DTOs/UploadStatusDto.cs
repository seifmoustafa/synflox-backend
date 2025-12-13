namespace Application.DTOs;

public record UploadStatusDto(int Parts, long Bytes, long ChunkSize);
