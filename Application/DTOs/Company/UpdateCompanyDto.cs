using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Company;

public class UpdateCompanyDto
{
    [StringLength(200, ErrorMessage = "Company name cannot exceed 200 characters")]
    public string? Name { get; set; }

    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
    public string? ContactEmail { get; set; }

    [StringLength(50, ErrorMessage = "Phone number cannot exceed 50 characters")]
    public string? ContactPhone { get; set; }

    [StringLength(500, ErrorMessage = "Address cannot exceed 500 characters")]
    public string? Address { get; set; }
}

