namespace Application.DTOs.Company;

/// <summary>
/// Response DTO for export file generation.
/// </summary>
public class ExportFileResponse
{
    /// <summary>
    /// The file request path (relative URL) to download the file.
    /// Example: "/files/companies_export_20250115120000.csv"
    /// </summary>
    public string FileRequestPath { get; set; } = string.Empty;

    /// <summary>
    /// Full download URL for the file.
    /// Example: "https://api.example.com/api/downloads/file?fileRequestPath=/files/companies_export_20250115120000.csv&scheme=AllFileSettings"
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Direct file URL (if using static file serving).
    /// Example: "/files/companies_export_20250115120000.csv"
    /// </summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>
    /// File name.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// File format (csv, xlsx).
    /// </summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>
    /// Expires at timestamp (optional - for auto-cleanup).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}

