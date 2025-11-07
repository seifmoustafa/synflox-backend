using System;
using Domain.Enums;

namespace Application.DTOs.Licensing;

public class LicenseKeyValidationResponse
{
    public bool IsValid { get; set; }
    public LicenseStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ClockTampered { get; set; }
    public Guid? CompanyId { get; set; }
}

