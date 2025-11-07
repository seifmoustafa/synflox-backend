using System;
using Application.DTOs;

namespace Application.DTOs.User;

public class UpdateProfileRequest : UserEditableBase
{
    public UploadReferenceDto? Image { get; set; }
    public string? ImageUrl { get; set; }
}
