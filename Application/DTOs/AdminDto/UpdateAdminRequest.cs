using System;

namespace Application.DTOs.Admin;

public class UpdateAdminRequest
{
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid? AdminTypeId { get; set; }
}
