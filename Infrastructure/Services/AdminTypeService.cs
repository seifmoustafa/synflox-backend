using System;
using System.Collections.Generic;
using AutoMapper;
using Application.DTOs.AdminType;
using Application.Services;
using Domain.Entities.Authentication;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class AdminTypeService : IAdminTypeService
{
    private readonly IAdminTypeRepository _repo;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public AdminTypeService(IAdminTypeRepository repo, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<AdminTypeDto>> GetAllAsync()
    {
        var entities = await _repo.GetAllAsync();
        return _mapper.Map<IEnumerable<AdminTypeDto>>(entities);
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
}

