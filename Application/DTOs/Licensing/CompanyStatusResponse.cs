using System;
using Domain.Enums;

namespace Application.DTOs.Licensing;

public class CompanyStatusResponse
{
    public LicenseStatus Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
}

