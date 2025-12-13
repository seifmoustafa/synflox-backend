namespace Infrastructure.Settings;

public class FileSettings
{
    public string[] AllowedExtensions { get; set; } = System.Array.Empty<string>();
    public string? StoragePath { get; set; }
    public string? RequestPath { get; set; }
    public int MaxFileSizeMb { get; set; }
    public long MaxFileSizeBytes => (long)MaxFileSizeMb * 1024 * 1024;
}
