using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Application.DTOs.AdminType;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Interfaces;
using Domain.Entities.Common;
using Domain.Exceptions;
using Microsoft.Extensions.Localization;
using Infrastructure.Resources;

namespace Infrastructure.Services;

public class AdminTypeService : IAdminTypeService
{
    private readonly IAdminTypeRepository _repo;
    private readonly IAdminRepository _adminRepo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AdminTypeService(
        IAdminTypeRepository repo, 
        IAdminRepository adminRepo,
        IMapper mapper, 
        IUnitOfWork unitOfWork,
        IStringLocalizer<SharedResource> localizer)
    {
        _repo = repo;
        _adminRepo = adminRepo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<IEnumerable<AdminTypeDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<AdminTypeDto>>(entities);
    }

    public async Task<(IEnumerable<AdminTypeDto>, PaginationMetadata)> GetAllAsync(int page, int pageSize, string? search)
    {
        // Use repository's paginated method with search on AdminTypeName
        var (entities, metadata) = await _repo.GetAllAsync(
            includes: null,
            pageNumber: page,
            pageSize: pageSize,
            search: search,
            searchColumns: at => at.AdminTypeName
        );

        var dtos = _mapper.Map<IEnumerable<AdminTypeDto>>(entities);
        return (dtos, metadata);
    }

    public async Task<AdminTypeDto?> GetByIdAsync(GetAdminTypeByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var entity = await _repo.GetByIdAsync(decryptedId, null);
        return entity is null ? null : _mapper.Map<AdminTypeDto>(entity);
    }

    public async Task<AdminTypeDto> CreateAsync(CreateAdminTypeDto dto)
    {
        var entity = _mapper.Map<AdminType>(dto);
        var created = await _repo.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<AdminTypeDto>(created);
    }

    public async Task<AdminTypeDto?> UpdateAsync(UpdateAdminTypeByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var entity = await _repo.GetByIdAsync(decryptedId, null);
        if (entity is null) return null;
        _mapper.Map(request.UpdateData, entity);
        await _repo.UpdateAsync(entity);
        await _unitOfWork.SaveChangesAsync();
        return _mapper.Map<AdminTypeDto>(entity);
    }

    public async Task DeleteAsync(DeleteAdminTypeRequest request)
    {
        // Decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        // Check if admin type exists
        var adminType = await _repo.GetByIdAsync(decryptedId, null);
        if (adminType is null)
        {
            throw new NotFoundException(_localizer["AdminType.NotFound"]);
        }

        // Check if any admins are using this admin type (using FindAsync with predicate)
        var adminsWithType = await _adminRepo.FindAsync(
            a => a.AdminTypeId == decryptedId && !a.IsDeleted
        );

        if (adminsWithType.Any())
        {
            var count = adminsWithType.Count();
            throw new BadRequestException(
                _localizer["AdminType.CannotDelete", count]
            );
        }

        // Safe to delete (soft delete) - DeleteAsync expects the ID, not the entity
        await _repo.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
    }
}

