using System;

namespace Application.DTOs.Settings;

public class PasswordPolicyDto
{
    public Guid Id { get; set; }
    public int MinLength { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
    public bool RequireNumbers { get; set; }
    public bool RequireSpecialChars { get; set; }
    public int? MaxAgeDays { get; set; }
    public int? PreventReuseCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedTimestamp { get; set; }
    public DateTime? UpdatedTimestamp { get; set; }
}

