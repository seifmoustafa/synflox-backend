using AutoMapper;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Interfaces;
using Domain.Exceptions;
using Application.DTOs.User;
using System.Linq;
using System.Linq.Expressions;

namespace Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IFileService _fileService;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUserRepository repo, IPasswordHasher hasher, IMapper mapper,
        ILocalizationService localizer, IFileService fileService, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _hasher = hasher;
        _mapper = mapper;
        _localizer = localizer;
        _fileService = fileService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(IEnumerable<UserDto> Users, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (entities, meta) = await _repo.GetAllAsync(
            null,
            page,
            pageSize,
            search);
        var dtos = _mapper.Map<IEnumerable<UserDto>>(entities);
        return (dtos, meta);
    }

    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var user = await _repo.GetByIdAsync(id, null);
        if (user is null) return null;
        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = _mapper.Map<User>(dto);

        var existingUsernameUser = await _repo.GetByUserNameAsync(user.Username);
        if (existingUsernameUser != null)
            throw new BadRequestException(_localizer["UsernameTaken"]);

        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            var existingPhoneUser = await _repo.GetByPhoneNumberAsync(user.PhoneNumber);
            if (existingPhoneUser != null)
                throw new BadRequestException(_localizer["PhoneTaken"]);
        }

        if (!string.IsNullOrEmpty(user.Email))
        {
            var existingEmailUser = await _repo.GetByEmailAsync(user.Email);
            if (existingEmailUser != null)
                throw new BadRequestException(_localizer["EmailTaken"]);
        }

        user.Password = _hasher.HashPassword(user.Password);
        var created = await _repo.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<UserDto>(created);
    }

    public async Task<UserDto?> UpdateAsync(UpdateUserDto dto)
    {
        var user = await _repo.GetByIdAsync(dto.Id, null);
        if (user is null) return null;

        if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
        {
            var existingPhoneUser = await _repo.GetByPhoneNumberAsync(dto.PhoneNumber);
            if (existingPhoneUser != null && existingPhoneUser.Id != dto.Id)
                throw new BadRequestException(_localizer["PhoneTaken"]);
        }

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
        {
            var existingUsernameUser = await _repo.GetByUserNameAsync(dto.Username);
            if (existingUsernameUser != null && existingUsernameUser.Id != dto.Id)
                throw new BadRequestException(_localizer["UsernameTaken"]);

            if (!string.IsNullOrEmpty(user.ImagePath))
            {
                user.ImagePath = await _fileService.RenameFileAsync(user.ImagePath, "Image", dto.Username);
            }
        }

        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
        {
            var existingEmailUser = await _repo.GetByEmailAsync(dto.Email);
            if (existingEmailUser != null && existingEmailUser.Id != dto.Id)
                throw new BadRequestException(_localizer["EmailTaken"]);
        }

        var password = user.Password;
        _mapper.Map(dto, user);
        if (!string.IsNullOrEmpty(dto.Password))
            user.Password = _hasher.HashPassword(dto.Password);
        else
            user.Password = password;
        await _repo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _repo.GetByIdAsync(id, null);
        if (user is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        if (!string.IsNullOrEmpty(user.ImagePath))
        {
            await _fileService.DeleteFileAsync(user.ImagePath, "Image");
        }

        await _repo.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        var user = await _repo.GetByIdAsync(dto.Id, null);
        if (user is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        bool valid = _hasher.VerifyPassword(dto.CurrentPassword, user.Password);
        if (!valid)
            throw new BadRequestException(_localizer["InvalidCurrentPassword"]);

        user.Password = _hasher.HashPassword(dto.NewPassword);
        await _repo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _repo.GetByIdAsync(dto.Id, null);
        if (user is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        user.Password = _hasher.HashPassword(dto.NewPassword);
        await _repo.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var user = await _repo.GetByIdAsync(id, null);
        if (user is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        if (!user.IsActive)
        {
            user.IsActive = true;
            await _repo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var user = await _repo.GetByIdAsync(id, null);
        if (user is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        if (user.IsActive)
        {
            user.IsActive = false;
            await _repo.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> ActivateSelectedAsync(IEnumerable<Guid> ids)
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.FindAsync(u => ids.Contains(u.Id), ct);
            var toActivate = users.Where(u => !u.IsActive).ToList();
            foreach (var user in toActivate)
            {
                user.IsActive = true;
            }
            if (toActivate.Any())
                await _repo.UpdateRangeAsync(toActivate, ct);
            affected = toActivate.Count;
        });
        return affected;
    }

    public async Task<int> DeactivateSelectedAsync(IEnumerable<Guid> ids)
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.FindAsync(u => ids.Contains(u.Id), ct);
            var toDeactivate = users.Where(u => u.IsActive).ToList();
            foreach (var user in toDeactivate)
            {
                user.IsActive = false;
            }
            if (toDeactivate.Any())
                await _repo.UpdateRangeAsync(toDeactivate, ct);
            affected = toDeactivate.Count;
        });
        return affected;
    }

    public async Task<int> ActivateAllAsync()
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.GetAllAsync(ct);
            var toActivate = users.Where(u => !u.IsActive).ToList();
            foreach (var user in toActivate)
            {
                user.IsActive = true;
            }
            if (toActivate.Any())
                await _repo.UpdateRangeAsync(toActivate, ct);
            affected = toActivate.Count;
        });
        return affected;
    }

    public async Task<int> DeactivateAllAsync()
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.GetAllAsync(ct);
            var toDeactivate = users.Where(u => u.IsActive).ToList();
            foreach (var user in toDeactivate)
            {
                user.IsActive = false;
            }
            if (toDeactivate.Any())
                await _repo.UpdateRangeAsync(toDeactivate, ct);
            affected = toDeactivate.Count;
        });
        return affected;
    }

    public async Task<int> DeleteSelectedAsync(IEnumerable<Guid> ids)
    {
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.FindAsync(u => ids.Contains(u.Id), ct);
            var deleteIds = new List<Guid>();
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.ImagePath))
                {
                    await _fileService.DeleteFileAsync(user.ImagePath, "Image");
                }
                deleteIds.Add(user.Id);
            }
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }

    public async Task<int> DeleteAllAsync()
    {
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var users = await _repo.GetAllAsync(ct);
            var deleteIds = new List<Guid>();
            foreach (var user in users)
            {
                if (!string.IsNullOrEmpty(user.ImagePath))
                {
                    await _fileService.DeleteFileAsync(user.ImagePath, "Image");
                }
                deleteIds.Add(user.Id);
            }
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }
}
