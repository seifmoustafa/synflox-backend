using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.AdminType;

public class CreateAdminTypeDto
{
    [Required]
    [StringLength(100)]
    public string AdminTypeName { get; set; } = string.Empty;
}

