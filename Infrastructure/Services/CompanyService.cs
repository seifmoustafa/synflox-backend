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
    private readonly ISubscriptionHistoryService _historyService;
    private readonly IWebhookService _webhookService;
    private readonly IIdEncryptionService _idEncryption;

    public CompanyService(
        ICompanyRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ISubscriptionHistoryService historyService,
        IWebhookService webhookService,
        IIdEncryptionService idEncryption)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _historyService = historyService;
        _webhookService = webhookService;
        _idEncryption = idEncryption;
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

        // Log to history
        await _historyService.LogSubscriptionEventAsync(
            created.Id,
            Domain.Enums.SubscriptionHistoryActionType.Created,
            null,
            new { Name = created.Name, IsActive = created.IsActive, ExpiryDate = created.ExpiryDate },
            _currentUserService.UserId,
            $"Company created: {created.Name}");

        // Trigger webhook
        // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
        // This is an exception to the rule - encryption here is acceptable for external API contracts
        try
        {
            var webhookPayload = new { CompanyId = _idEncryption.Encrypt(created.Id), CompanyName = created.Name, EventType = "CompanyCreated" };
            await _webhookService.TriggerWebhookAsync(created.Id, Domain.Enums.WebhookEventType.CompanyCreated, webhookPayload);
        }
        catch (Exception webhookEx)
        {
            // Log but don't fail the operation
        }

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

        var oldValues = new
        {
            Name = company.Name,
            IsActive = company.IsActive,
            ExpiryDate = company.ExpiryDate,
            ContactEmail = company.ContactEmail,
            ContactPhone = company.ContactPhone,
            Address = company.Address
        };

        _mapper.Map(dto, company);
        await _repository.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        // Log to history
        await _historyService.LogSubscriptionEventAsync(
            company.Id,
            Domain.Enums.SubscriptionHistoryActionType.Updated,
            oldValues,
            new { Name = company.Name, IsActive = company.IsActive, ExpiryDate = company.ExpiryDate, ContactEmail = company.ContactEmail, ContactPhone = company.ContactPhone, Address = company.Address },
            _currentUserService.UserId,
            $"Company updated: {company.Name}");

        // Trigger webhook
        // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
        // This is an exception to the rule - encryption here is acceptable for external API contracts
        try
        {
            var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, EventType = "CompanyUpdated" };
            await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyUpdated, webhookPayload);
        }
        catch (Exception webhookEx)
        {
            // Log but don't fail the operation
        }

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

        // Log to history before deletion
        await _historyService.LogSubscriptionEventAsync(
            company.Id,
            Domain.Enums.SubscriptionHistoryActionType.Deleted,
            new { Name = company.Name, IsActive = company.IsActive, ExpiryDate = company.ExpiryDate },
            null,
            _currentUserService.UserId,
            $"Company deleted: {company.Name}");

        // Trigger webhook before deletion
        // NOTE: Webhook payloads encrypt IDs because they are sent to external systems
        // This is an exception to the rule - encryption here is acceptable for external API contracts
        try
        {
            var webhookPayload = new { CompanyId = _idEncryption.Encrypt(company.Id), CompanyName = company.Name, EventType = "CompanyDeleted" };
            await _webhookService.TriggerWebhookAsync(company.Id, Domain.Enums.WebhookEventType.CompanyDeleted, webhookPayload);
        }
        catch (Exception webhookEx)
        {
            // Log but don't fail the operation
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<BulkOperationResponse> BulkDeleteAsync(List<Guid> companyIds)
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = companyIds.Count
        };

        foreach (var companyId in companyIds)
        {
            try
            {
                await DeleteCompanyAsync(companyId);
                var company = await _repository.GetByIdAsync(companyId, null);
                response.Results.Add(new BulkOperationResult
                {
                    CompanyId = _idEncryption.Encrypt(companyId),
                    CompanyName = company?.Name ?? "Unknown",
                    Success = true
                });
                response.Successful++;
            }
            catch (Exception ex)
            {
                var company = await _repository.GetByIdAsync(companyId, null);
                response.Results.Add(new BulkOperationResult
                {
                    // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                    // This is manual encryption for response DTOs that don't go through mapper
                    CompanyId = _idEncryption.Encrypt(companyId),
                    CompanyName = company?.Name ?? "Unknown",
                    Success = false,
                    ErrorMessage = ex.Message
                });
                response.Failed++;
            }
        }

        return response;
    }

    public async Task<BulkOperationResponse> BulkUpdateAsync(List<Guid> companyIds, UpdateCompanyDto updateDto)
    {
        var response = new BulkOperationResponse
        {
            TotalRequested = companyIds.Count
        };

        foreach (var companyId in companyIds)
        {
            try
            {
                await UpdateCompanyAsync(companyId, updateDto);
                var company = await _repository.GetByIdAsync(companyId, null);
                response.Results.Add(new BulkOperationResult
                {
                    CompanyId = _idEncryption.Encrypt(companyId),
                    CompanyName = company?.Name ?? "Unknown",
                    Success = true
                });
                response.Successful++;
            }
            catch (Exception ex)
            {
                var company = await _repository.GetByIdAsync(companyId, null);
                response.Results.Add(new BulkOperationResult
                {
                    // Encrypt ID for response DTO (Entity → DTO encryption handled by mapper)
                    // This is manual encryption for response DTOs that don't go through mapper
                    CompanyId = _idEncryption.Encrypt(companyId),
                    CompanyName = company?.Name ?? "Unknown",
                    Success = false,
                    ErrorMessage = ex.Message
                });
                response.Failed++;
            }
        }

        return response;
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

