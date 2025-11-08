using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Licensing;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class CompanyCustomFieldService : ICompanyCustomFieldService
{
    private readonly ICompanyCustomFieldRepository _repository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    public CompanyCustomFieldService(
        ICompanyCustomFieldRepository repository,
        ICompanyRepository companyRepository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _companyRepository = companyRepository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyCustomFieldDto> CreateFieldAsync(CreateCompanyCustomFieldDto request)
    {
        // Check if company exists
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, null);
        if (company == null || company.IsDeleted)
            throw new NotFoundException(_localizer["Company.NotFound"]);

        // Check if field with same name already exists
        var existing = await _repository.GetByCompanyIdAndFieldNameAsync(request.CompanyId, request.FieldName);
        if (existing != null && !existing.IsDeleted)
            throw new BadRequestException(_localizer["CompanyCustomField.AlreadyExists"]);

        var field = _mapper.Map<CompanyCustomField>(request);
        var created = await _repository.AddAsync(field);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyCustomFieldDto>(created);
    }

    public async Task<IEnumerable<CompanyCustomFieldDto>> GetFieldsByCompanyIdAsync(Guid companyId)
    {
        var fields = await _repository.GetByCompanyIdAsync(companyId);
        return _mapper.Map<IEnumerable<CompanyCustomFieldDto>>(fields);
    }

    public async Task<CompanyCustomFieldDto?> GetFieldByIdAsync(Guid fieldId)
    {
        var field = await _repository.GetByIdAsync(fieldId, null);
        if (field == null || field.IsDeleted)
            return null;

        return _mapper.Map<CompanyCustomFieldDto>(field);
    }

    public async Task<CompanyCustomFieldDto> UpdateFieldAsync(Guid fieldId, UpdateCompanyCustomFieldDto request)
    {
        var field = await _repository.GetByIdAsync(fieldId, null);
        if (field == null || field.IsDeleted)
            throw new NotFoundException(_localizer["CompanyCustomField.NotFound"]);

        _mapper.Map(request, field);
        await _repository.UpdateAsync(field);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyCustomFieldDto>(field);
    }

    public async Task DeleteFieldAsync(Guid fieldId)
    {
        var field = await _repository.GetByIdAsync(fieldId, null);
        if (field == null || field.IsDeleted)
            throw new NotFoundException(_localizer["CompanyCustomField.NotFound"]);

        await _repository.DeleteAsync(fieldId);
        await _unitOfWork.SaveChangesAsync();
    }
}


