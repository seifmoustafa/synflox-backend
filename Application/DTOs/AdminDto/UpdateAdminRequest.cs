using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Admin;

public class UpdateAdminRequest
{
    [StringLength(100, MinimumLength = 3)]
    public string? Username { get; set; }
    
    [StringLength(100)]
    public string? FirstName { get; set; }
    
    [StringLength(100)]
    public string? LastName { get; set; }
    
    [Phone]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
    
    public Guid AdminTypeId { get; set; }
}
