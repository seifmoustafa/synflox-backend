namespace Application.DTOs.Authentication;

using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Application.DTOs;

public class RegistrationRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? NationalId { get; set; }
    public Gender? Gender { get; set; }
    public UploadReferenceDto? Image { get; set; }
    public string? ImageUrl { get; set; }

    [Required, Phone]
    public string PhoneNumber { get; set; } = string.Empty;

    public string? Country { get; set; }
    public string? Government { get; set; }
    public string? City { get; set; }
}
