using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.AdminType;

public class UpdateAdminTypeDto
{
    [StringLength(100)]
    public string? AdminTypeName { get; set; }
}

