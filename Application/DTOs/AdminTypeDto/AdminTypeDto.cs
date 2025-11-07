using System;

namespace Application.DTOs.AdminType;

public class AdminTypeDto
{
    public Guid Id { get; set; }
    public string AdminTypeName { get; set; } = string.Empty;
}

