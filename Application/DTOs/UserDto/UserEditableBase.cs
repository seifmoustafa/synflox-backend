using System;
using Domain.Enums;

namespace Application.DTOs.User;

public class UserEditableBase
{
    public string? FirstName { get; set; }
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? NationalId { get; set; }
    public Gender? Gender { get; set; }
    public string? Country { get; set; }
    public string? Government { get; set; }
    public string? City { get; set; }
}
