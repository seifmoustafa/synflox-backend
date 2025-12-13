using System;

namespace Application.DTOs.OnlineAccess;

/// <summary>
/// Request DTO for operations requiring a token ID.
/// Used for AutoMapper decryption.
/// </summary>
public class TokenIdRequest
{
    public Guid TokenId { get; set; }
}

/// <summary>
/// Request DTO for operations requiring a device ID.
/// Used for AutoMapper decryption.
/// </summary>
public class DeviceIdRequest
{
    public Guid DeviceId { get; set; }
}
