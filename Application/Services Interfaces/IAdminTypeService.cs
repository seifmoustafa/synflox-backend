using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.AdminType;

namespace Application.Services;

public interface IAdminTypeService
{
    Task<IEnumerable<AdminTypeDto>> GetAllAsync();
    Task<AdminTypeDto?> GetByIdAsync(GetAdminTypeByIdRequest request);
    Task<AdminTypeDto> CreateAsync(CreateAdminTypeDto dto);
    Task<AdminTypeDto?> UpdateAsync(UpdateAdminTypeByIdRequest request);
}

