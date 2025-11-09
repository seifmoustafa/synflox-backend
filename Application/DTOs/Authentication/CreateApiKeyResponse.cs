using System.Text.Json.Serialization;

namespace Application.DTOs.Authentication;

/// <summary>
/// Response DTO containing the newly created API key (only shown once).
/// </summary>
public class CreateApiKeyResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;
    
    [JsonPropertyName("keyPrefix")]
    public string KeyPrefix { get; set; } = string.Empty;
    
    [JsonPropertyName("signingSecret")]
    public string? SigningSecret { get; set; } // Only returned when creating/regenerating
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

