using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.AdminType;

namespace Application.Services;

public interface IAdminTypeService
{
    Task<IEnumerable<AdminTypeDto>> GetAllAsync();
    Task<AdminTypeDto?> GetByIdAsync(Guid id);
    Task<AdminTypeDto> CreateAsync(CreateAdminTypeDto dto);
    Task<AdminTypeDto?> UpdateAsync(Guid id, UpdateAdminTypeDto dto);
}

