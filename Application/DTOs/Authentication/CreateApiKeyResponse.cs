namespace Application.DTOs.Authentication;

/// <summary>
/// Response DTO containing the newly created API key (only shown once).
/// </summary>
public class CreateApiKeyResponse
{
    public Guid Id { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = string.Empty;
    public string? SigningSecret { get; set; } // Only returned when creating/regenerating
    public string Message { get; set; } = string.Empty;
}

