using AutoMapper;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Entities.Common;
using Domain.Interfaces;
using Domain.Exceptions;
using Application.DTOs.Admin;
using System.Linq.Expressions;
using System.Linq;

namespace Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly IAdminRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdEncryptionService _idEncryption;

    public AdminService(IAdminRepository repo, IPasswordHasher hasher, IMapper mapper,
        ILocalizationService localizer, IUnitOfWork unitOfWork, IIdEncryptionService idEncryption)
    {
        _repo = repo;
        _hasher = hasher;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _idEncryption = idEncryption;
    }

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

    public async Task<AdminDto?> GetByIdAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (admin is null) return null;
        return _mapper.Map<AdminDto>(admin);
    }

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

    public async Task<AdminDto?> UpdateAsync(Guid id, UpdateAdminRequest dto)
    {
        var admin = await _repo.GetByIdAsync(id, ["AdminType"]);
        if (admin is null) return null;

        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != admin.Username)
        {
            var existing = await _repo.GetByUserNameAsync(dto.Username);
            if (existing != null && existing.Id != id)
                throw new BadRequestException(_localizer["UsernameTaken"]);
        }

        _mapper.Map(dto, admin);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
        admin = await _repo.GetByIdAsync(id, ["AdminType"]);
        return _mapper.Map<AdminDto>(admin);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);

        await _repo.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        bool valid = _hasher.VerifyPassword(currentPassword, admin.Password);
        if (!valid)
            throw new BadRequestException(_localizer["InvalidCurrentPassword"]);

        admin.Password = _hasher.HashPassword(newPassword);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task ResetPasswordAsync(Guid id, string newPassword)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        admin.Password = _hasher.HashPassword(newPassword);
        await _repo.UpdateAsync(admin);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<bool> ActivateAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, null);
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

    public async Task<bool> DeactivateAsync(Guid id)
    {
        var admin = await _repo.GetByIdAsync(id, null);
        if (admin is null)
            throw new NotFoundException(_localizer["UserNotFound"]);
        if (admin.IsActive)
        {
            admin.IsActive = false;
            await _repo.UpdateAsync(admin);
            await _unitOfWork.SaveChangesAsync();
        }
        return true;
    }

    public async Task<int> ActivateSelectedAsync(IEnumerable<Guid> ids)
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => ids.Contains(a.Id), ct);
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

    public async Task<int> DeactivateSelectedAsync(IEnumerable<Guid> ids)
    {
        int affected = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => ids.Contains(a.Id), ct);
            var toDeactivate = admins.Where(a => a.IsActive).ToList();
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
            var toDeactivate = admins.Where(a => a.IsActive).ToList();
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

    public async Task<int> DeleteSelectedAsync(IEnumerable<Guid> ids)
    {
        int deleted = 0;
        await _unitOfWork.ExecuteAsync(async ct =>
        {
            var admins = await _repo.FindAsync(a => ids.Contains(a.Id), ct);
            var deleteIds = admins.Select(a => a.Id).ToList();
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
            var deleteIds = admins.Select(a => a.Id).ToList();
            if (deleteIds.Any())
                await _repo.DeleteRangeAsync(deleteIds, ct);
            deleted = deleteIds.Count;
        });
        return deleted;
    }
}
