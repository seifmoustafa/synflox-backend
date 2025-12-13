using Application.DTOs.Admin;
using Application.Services;
using AutoMapper;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Exceptions;
using Domain.Interfaces;
using Infrastructure.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing admin CRUD operations (SuperAdmin only)
/// Handles create, read, update, delete, activate, deactivate operations
/// Separated from AdminProfileService for Single Responsibility Principle
/// </summary>
public class AdminCrudService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    // Root SuperAdmin username - cannot be deleted
    private const string ROOT_SUPERADMIN_USERNAME = "superadmin";

    public AdminCrudService(
        IAdminRepository repo,
        IPasswordHasher hasher,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _hasher = hasher;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    // ===== Read Operations =====

    public async Task<(IEnumerable<AdminDto> Admins, PaginationMetadata Meta)> GetAllAsync(int page, int pageSize, string? search)
    {
        var (entities, meta) = await _repo.GetAllAsync(
            ["AdminType"],
            page,
            pageSize,
            search);
        var dtos = _mapper.Map<IEnumerable<AdminDto>>(entities);
        return (dtos, meta);
    }

    public async Task<AdminDto?> GetByIdAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        var admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        if (admin is null) return null;
        return _mapper.Map<AdminDto>(admin);
    }

    /// <summary>
    /// Direct overload for internal calls (from JWT) - accepts decrypted Guid
    /// NO DECRYPTION NEEDED - ID is already decrypted from JWT
    /// </summary>
    public async Task<AdminDto?> GetByIdAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (admin is null) return null;
        return _mapper.Map<AdminDto>(admin);
    }

    // ===== Create Operation =====

    public async Task<AdminDto> CreateAsync(CreateAdminDto dto)
    {
        var admin = _mapper.Map<Admin>(dto);

        var existing = await _repo.GetByUserNameAsync(admin.Username);
        if (existing != null)
            throw new BadRequestException(_localizer["UsernameTaken"]);

        admin.Password = _hasher.HashPassword(admin.Password);
        var created = await _repo.AddAsync(admin);
        await _unitOfWork.SaveChangesAsync();
        var withType = await _repo.GetByIdAsync(created.Id, ["AdminType"]);
        return _mapper.Map<AdminDto>(withType);
    }

    // ===== Update Operations =====

    public async Task<AdminDto?> UpdateAsync(UpdateAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        var admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        if (admin is null) return null;

        // Protect root superadmin from username change
        if (admin.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
        {
            if (request.UpdateData.Username != null && !request.UpdateData.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(_localizer["SuperAdmin.CannotChangeUsername"]);
            }
        }

        if (!string.IsNullOrEmpty(request.UpdateData.Username) && request.UpdateData.Username != admin.Username)
        {
            var existing = await _repo.GetByUserNameAsync(request.UpdateData.Username);
            if (existing != null && existing.Id != decryptedId)
                throw new BadRequestException(_localizer["UsernameTaken"]);
        }

        _mapper.Map(request.UpdateData, admin);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
        admin = await _repo.GetByIdAsync(decryptedId, ["AdminType"]);
        return _mapper.Map<AdminDto>(admin);
    }

    /// <summary>
    /// Direct overload for internal calls - accepts decrypted Guid
    /// </summary>
    public async Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest data)
    {
        var entity = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (entity is null) return null;

        // Protect root superadmin from username change
        if (entity.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
        {
            if (data.Username != null && !data.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException(_localizer["SuperAdmin.CannotChangeUsername"]);
            }
        }

        if (!string.IsNullOrEmpty(data.Username) && data.Username != entity.Username)
        {
            var existing = await _repo.GetByUserNameAsync(data.Username);
            if (existing != null && existing.Id != id)
                throw new BadRequestException(_localizer["UsernameTaken"]);
        }

        _mapper.Map(data, entity);
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        entity = await _repo.GetByIdAsync(id, ["AdminType"]);
        return _mapper.Map<AdminDto>(entity);
    }

    // ===== Password Operations =====

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        bool valid = _hasher.VerifyPassword(currentPassword, admin.Password);
        if (!valid)
            throw new BadRequestException(_localizer["InvalidCurrentPassword"]);

        admin.Password = _hasher.HashPassword(newPassword);
        admin.LastPasswordChangeAt = DateTime.UtcNow;

        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(ChangePasswordByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        admin.Password = _hasher.HashPassword(request.NewPassword);
        admin.LastPasswordChangeAt = DateTime.UtcNow;
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    // ===== Activate/Deactivate Operations =====

    public async Task<bool> ActivateAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        if (!admin.IsActive)
        {
            admin.IsActive = true;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }

    public async Task<bool> DeactivateAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        // Protect root superadmin from deactivation
        if (admin.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException(_localizer["SuperAdmin.CannotDeactivate"]);
        }

        if (admin.IsActive)
        {
            admin.IsActive = false;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> ActivateSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs - SYNFLOX ID ENCRYPTION RULE
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);

        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            var toActivate = admins.Where(a => !a.IsActive).ToList();
            foreach (var admin in toActivate)
            {
                admin.IsActive = true;
            }
            if (toActivate.Any())
                await _repo.UpdateRangeAsync(toActivate, ct);
            affected = toActivate.Count;
        });
        return affected;
    }

    public async Task<int> DeactivateSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs - SYNFLOX ID ENCRYPTION RULE
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);

        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            
            // Filter out root superadmin - PROTECTION
            var toDeactivate = admins
                .Where(a => a.IsActive && !a.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var admin in toDeactivate)
            {
                admin.IsActive = false;
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
            var admins = await _repo.GetAllAsync(ct);
            var toActivate = admins.Where(a => !a.IsActive).ToList();
            foreach (var admin in toActivate)
            {
                admin.IsActive = true;
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
            var admins = await _repo.GetAllAsync(ct);
            
            // Filter out root superadmin - PROTECTION
            var toDeactivate = admins
                .Where(a => a.IsActive && !a.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            foreach (var admin in toDeactivate)
            {
                admin.IsActive = false;
            }
            if (toDeactivate.Any())
                await _repo.UpdateRangeAsync(toDeactivate, ct);
            affected = toDeactivate.Count;
        });
        return affected;
    }

    // ===== Delete Operations =====

    public async Task<bool> DeleteAsync(GetAdminByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID - SYNFLOX ID ENCRYPTION RULE
        var decryptedId = _mapper.Map<Guid>(request);

        // Protect root superadmin from deletion
        var admin = await _repo.GetByIdAsync(decryptedId, null);
        if (admin == null)
            throw new NotFoundException(_localizer["Admin.NotFound"]);
            
        if (admin.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
            throw new BadRequestException(_localizer["SuperAdmin.CannotDelete"]);

        // ⭐ CASCADE: Delete auth records (refresh tokens, backup codes, reset tokens)
        await _repo.SoftDeleteAuthRecordsAsync(decryptedId);

        await _repo.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteSelectedAsync(AdminIdsRequest request)
    {
        // Use AutoMapper to decrypt the IDs - SYNFLOX ID ENCRYPTION RULE
        var decryptedIds = _mapper.Map<IEnumerable<Guid>>(request);

        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => decryptedIds.Contains(a.Id), ct);
            
            // Filter out root superadmin - PROTECTION
            var deleteIds = admins
                .Where(a => !a.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Id)
                .ToList();
            
            // ⭐ CASCADE: Delete auth records for each admin
            foreach (var adminId in deleteIds)
            {
                await _repo.SoftDeleteAuthRecordsAsync(adminId, ct);
            }
            
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }

    public async Task<int> DeleteAllExceptAsync(Guid exceptId)
    {
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => a.Id != exceptId, ct);
            
            // Filter out root superadmin - PROTECTION
            var deleteIds = admins
                .Where(a => !a.Username.Equals(ROOT_SUPERADMIN_USERNAME, StringComparison.OrdinalIgnoreCase))
                .Select(a => a.Id)
                .ToList();
            
            // ⭐ CASCADE: Delete auth records for each admin
            foreach (var adminId in deleteIds)
            {
                await _repo.SoftDeleteAuthRecordsAsync(adminId, ct);
            }
            
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }
}
