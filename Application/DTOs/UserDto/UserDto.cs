using System;
namespace Application.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }
    public string? NationalId { get; set; }

    public string? Gender { get; set; }

    public string? ImagePath { get; set; }
    public string? Country { get; set; }
    public string? Government { get; set; }
    public string? City { get; set; }
    public string? Providers { get; set; }
    public bool IsEmailVerified { get; set; }
    public bool IsPhoneVerified { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLogin { get; set; }

}
