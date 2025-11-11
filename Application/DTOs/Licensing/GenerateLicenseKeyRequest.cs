using System;

namespace Application.DTOs.Licensing;

public class GenerateLicenseKeyRequest
{
    public Guid CompanyId { get; set; }
}
