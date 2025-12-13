using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs.AdminType;
using Domain.Entities.Common;

namespace Application.Services;

public interface IAdminTypeService
{
    Task<IEnumerable<AdminTypeDto>> GetAllAsync();
    Task<(IEnumerable<AdminTypeDto>, PaginationMetadata)> GetAllAsync(int page, int pageSize, string? search);
    Task<AdminTypeDto?> GetByIdAsync(GetAdminTypeByIdRequest request);
    Task<AdminTypeDto> CreateAsync(CreateAdminTypeDto dto);
    Task<AdminTypeDto?> UpdateAsync(UpdateAdminTypeByIdRequest request);
    Task DeleteAsync(DeleteAdminTypeRequest request);
}

