using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public CompanyService(
        ICompanyRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto)
    {
        // Check if company name already exists
        var existing = await _repository.GetByNameAsync(dto.Name);
        if (existing != null)
        {
            throw new BadRequestException(_localizer["Company.CompanyNameExists"]);
        }

        var company = _mapper.Map<Company>(dto);
        var created = await _repository.AddAsync(company);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<CompanyDto>(created);
        SetLicenseKeyIfSuperAdmin(result, created);
        return result;
    }

    public async Task<(IEnumerable<CompanyDto> Companies, PaginationMetadata Meta)> GetAllCompaniesAsync(
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        Expression<Func<Company, object?>>[] searchColumns = 
        {
            c => c.Name,
            c => c.ContactEmail,
            c => c.ContactPhone,
            c => c.Address
        };

        var (entities, meta) = await _repository.GetAllAsync(
            includes: null,
            pageNumber: page,
            pageSize: pageSize,
            search: search,
            cancellationToken: default,
            searchColumns: searchColumns);

        var companyList = entities.ToList();
        var dtos = new List<CompanyDto>();
        
        // Map each company and conditionally set LicenseKey based on user role
        foreach (var company in companyList)
        {
            var dto = _mapper.Map<CompanyDto>(company);
            SetLicenseKeyIfSuperAdmin(dto, company);
            dtos.Add(dto);
        }
        
        return (dtos, meta);
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(Guid id)
    {
        var company = await _repository.GetByIdAsync(id, null);
        if (company == null) return null;
        var dto = _mapper.Map<CompanyDto>(company);
        SetLicenseKeyIfSuperAdmin(dto, company);
        return dto;
    }

    public async Task<CompanyDto?> UpdateCompanyAsync(Guid id, UpdateCompanyDto dto)
    {
        var company = await _repository.GetByIdAsync(id, null);
        if (company == null)
        {
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
        }

        // Check name uniqueness if name is being updated
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != company.Name)
        {
            var existing = await _repository.GetByNameAsync(dto.Name);
            if (existing != null && existing.Id != id)
            {
                throw new BadRequestException(_localizer["Company.CompanyNameExists"]);
            }
        }

        _mapper.Map(dto, company);
        await _repository.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<CompanyDto>(company);
        SetLicenseKeyIfSuperAdmin(result, company);
        return result;
    }

    public async Task<bool> DeleteCompanyAsync(Guid id)
    {
        var company = await _repository.GetByIdAsync(id, null);
        if (company == null)
        {
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Sets LicenseKey in the DTO only if the current user is SuperAdmin.
    /// </summary>
    private void SetLicenseKeyIfSuperAdmin(CompanyDto dto, Company company)
    {
        if (_currentUserService.AdminTypeName == "SuperAdmin")
        {
            dto.LicenseKey = company.LicenseKey;
        }
    }
}

