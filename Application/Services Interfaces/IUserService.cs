using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Domain.Entities.Common;
using Application.DTOs.User;

namespace Application.Services;

public interface IUserService
{
    Task<(IEnumerable<UserDto> Users, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search);
    Task<UserDto?> GetByIdAsync(Guid id);
    Task<UserDto> CreateAsync(CreateUserDto dto);
    Task<UserDto?> UpdateAsync(UpdateUserDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<int> DeleteSelectedAsync(IEnumerable<Guid> ids);
    Task<int> DeleteAllAsync();
    Task<bool> ActivateAsync(Guid id);
    Task<bool> DeactivateAsync(Guid id);
    Task<int> ActivateSelectedAsync(IEnumerable<Guid> ids);
    Task<int> DeactivateSelectedAsync(IEnumerable<Guid> ids);
    Task<int> ActivateAllAsync();
    Task<int> DeactivateAllAsync();
    Task ChangePasswordAsync(ChangePasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
}
