using System.Collections.Generic;

namespace Application.DTOs.Licensing;

/// <summary>
/// Response DTO for bulk operations.
/// </summary>
public class BulkOperationResponse
{
    public int TotalRequested { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public List<BulkOperationResult> Results { get; set; } = new List<BulkOperationResult>();
}

/// <summary>
/// Result for a single company in a bulk operation.
/// </summary>
public class BulkOperationResult
{
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

