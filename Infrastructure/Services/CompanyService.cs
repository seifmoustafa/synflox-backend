using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.DTOs.Common;
using Application.DTOs.Company;
using Application.Services;
using Application.Services_Interfaces;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Enums;
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
    private readonly IEmailService _emailService;
    private readonly IActivityLogService _activityLogService;

    public CompanyService(
        ICompanyRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IActivityLogService activityLogService)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _activityLogService = activityLogService;
    }

    public async Task<CompanyDto> CreateCompanyAsync(CreateCompanyDto dto, string? language = null)
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
        
        // Log activity
        try
        {
            var currentUserId = _currentUserService.UserId;
            await _activityLogService.LogCompanyActivityAsync(
                ActivityActionType.Created,
                created.Id,
                created.Name,
                currentUserId != Guid.Empty ? currentUserId : null,
                null); // Let ActivityLogService fetch the username from DB
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log activity: {ex.Message}");
        }
        
        // Send welcome email
        try
        {
            await _emailService.SendCompanyCreatedEmailAsync(
                created.ContactEmail,
                created.Name,
                "System",
                language); // Uses request culture if null
        }
        catch (Exception ex)
        {
            // Log email error but don't fail the operation
            Console.WriteLine($"Failed to send company creation email: {ex.Message}");
        }
        
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
        var dtos = companyList.Select(company => _mapper.Map<CompanyDto>(company)).ToList();
        
        return (dtos, meta);
    }

    public async Task<CompanyDto?> GetCompanyByIdAsync(GetCompanyByIdRequest request)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var company = await _repository.GetByIdAsync(decryptedId, null);
        if (company == null) return null;
        var dto = _mapper.Map<CompanyDto>(company);
        return dto;
    }

    public async Task<CompanyDto?> UpdateCompanyAsync(UpdateCompanyByIdRequest request, string? language = null)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var company = await _repository.GetByIdAsync(decryptedId, null);
        if (company == null)
        {
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
        }

        // Track what fields are being updated
        var updatedFields = new List<string>();
        if (!string.IsNullOrEmpty(request.UpdateData.Name) && request.UpdateData.Name != company.Name)
            updatedFields.Add("Company Name");
        if (!string.IsNullOrEmpty(request.UpdateData.ContactEmail) && request.UpdateData.ContactEmail != company.ContactEmail)
            updatedFields.Add("Contact Email");
        if (!string.IsNullOrEmpty(request.UpdateData.ContactPhone) && request.UpdateData.ContactPhone != company.ContactPhone)
            updatedFields.Add("Contact Phone");
        if (!string.IsNullOrEmpty(request.UpdateData.Address) && request.UpdateData.Address != company.Address)
            updatedFields.Add("Address");

        // Check name uniqueness if name is being updated
        if (!string.IsNullOrEmpty(request.UpdateData.Name) && request.UpdateData.Name != company.Name)
        {
            var existing = await _repository.GetByNameAsync(request.UpdateData.Name);
            if (existing != null && existing.Id != decryptedId)
            {
                throw new BadRequestException(_localizer["Company.CompanyNameExists"]);
            }
        }

        _mapper.Map(request.UpdateData, company);
        await _repository.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();

        var result = _mapper.Map<CompanyDto>(company);
        
        // Send update email if any fields were changed
        if (updatedFields.Any())
        {
            try
            {
                await _emailService.SendCompanyUpdatedEmailAsync(
                    company.ContactEmail,
                    company.Name,
                    string.Join(", ", updatedFields),
                    language);
            }
            catch (Exception ex)
            {
                // Log email error but don't fail the operation
                Console.WriteLine($"Failed to send company update email: {ex.Message}");
            }
        }
        
        return result;
    }

    /// <inheritdoc />
    public async Task<DeletePreviewDto> GetDeletePreviewAsync(GetCompanyByIdRequest request)
    {
        var decryptedId = _mapper.Map<Guid>(request);
        var company = await _repository.GetByIdAsync(decryptedId, null);
        
        if (company == null)
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);

        var preview = new DeletePreviewDto
        {
            EntityType = "Company",
            EntityName = company.Name
        };

        // Get affected Subscriptions
        var totalSubscriptions = await _repository.GetSubscriptionsCountAsync(decryptedId);
        if (totalSubscriptions > 0)
        {
            var subNames = await _repository.GetSubscriptionNamesAsync(decryptedId, 10);
            preview.AffectedItems.Add(new AffectedItemGroup
            {
                ItemType = _localizer["Subscriptions"],
                Count = totalSubscriptions,
                ItemNames = subNames
            });
            preview.Warnings.Add(string.Format(_localizer["Company.SubscriptionsWillBeDeleted"], totalSubscriptions));
        }

        // Get affected Client Tokens
        var totalTokens = await _repository.GetClientTokensCountAsync(decryptedId);
        if (totalTokens > 0)
        {
            preview.AffectedItems.Add(new AffectedItemGroup
            {
                ItemType = _localizer["ClientTokens"],
                Count = totalTokens,
                ItemNames = new List<string>()
            });
            preview.Warnings.Add(string.Format(_localizer["Company.ClientTokensWillBeDeleted"], totalTokens));
        }

        preview.TotalAffectedCount = totalSubscriptions + totalTokens;
        preview.CanDelete = true;

        return preview;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteCompanyAsync(DeleteCompanyRequest request, bool confirmCascade = false, string? language = null)
    {
        var decryptedId = _mapper.Map<Guid>(request);
        
        var company = await _repository.GetByIdAsync(decryptedId, null);
        if (company == null)
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);

        // Check if cascade is needed
        var hasRelatedRecords = await _repository.HasRelatedRecordsAsync(decryptedId);

        // If there are related records and cascade not confirmed, throw
        if (hasRelatedRecords && !confirmCascade)
            throw new InvalidOperationException(_localizer["Company.HasRelatedRecords"]);

        // Store company info for email before deletion
        var companyName = company.Name;
        var contactEmail = company.ContactEmail;

        // ⭐ CASCADE 1: Soft delete all subscriptions (and their histories)
        await _repository.SoftDeleteSubscriptionsAsync(decryptedId);

        // ⭐ CASCADE 2: Soft delete all client tokens
        await _repository.SoftDeleteClientTokensAsync(decryptedId);

        // ⭐ CASCADE 3: Soft delete the company itself
        await _repository.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        
        // Send deletion email
        try
        {
            await _emailService.SendCompanyDeletedEmailAsync(contactEmail, companyName, language);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send company deletion email: {ex.Message}");
        }
        
        return true;
    }

    public async Task<bool> ActivateCompanyAsync(CompanyActionRequest request, string? language = null)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var company = await _repository.GetByIdAsync(decryptedId, null);
        if (company == null)
        {
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
        }

        // Activate company (assuming there's an IsActive property)
        company.IsActive = true;
        await _repository.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();
        
        // Send activation email if requested
        if (request.SendEmailNotification)
        {
            try
            {
                await _emailService.SendCompanyActivatedEmailAsync(company.ContactEmail, company.Name, language);
            }
            catch (Exception ex)
            {
                // Log email error but don't fail the operation
                Console.WriteLine($"Failed to send company activation email: {ex.Message}");
            }
        }
        
        return true;
    }

    public async Task<bool> DeactivateCompanyAsync(CompanyActionRequest request, string? language = null)
    {
        // Use AutoMapper to decrypt the ID
        var decryptedId = _mapper.Map<Guid>(request);
        
        var company = await _repository.GetByIdAsync(decryptedId, null);
        if (company == null)
        {
            throw new NotFoundException(_localizer["Company.CompanyNotFound"]);
        }

        // Deactivate company
        company.IsActive = false;
        await _repository.UpdateAsync(company);
        await _unitOfWork.SaveChangesAsync();
        
        // Send deactivation email if requested
        if (request.SendEmailNotification)
        {
            try
            {
                await _emailService.SendCompanyDeactivatedEmailAsync(
                    company.ContactEmail, 
                    company.Name, 
                    request.Reason,
                    language);
            }
            catch (Exception ex)
            {
                // Log email error but don't fail the operation
                Console.WriteLine($"Failed to send company deactivation email: {ex.Message}");
            }
        }
        
        return true;
    }

    public async Task<BulkOperationResult> BulkDeleteCompaniesAsync(BulkCompanyActionRequest request)
    {
        var result = new BulkOperationResult
        {
            TotalProcessed = request.CompanyIds.Count
        };

        foreach (var encryptedId in request.CompanyIds)
        {
            try
            {
                // Decrypt the ID
                var decryptedId = _mapper.Map<Guid>(new { CompanyId = encryptedId });
                
                var company = await _repository.GetByIdAsync(decryptedId, null);
                if (company == null)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        CompanyId = encryptedId,
                        CompanyName = "Unknown",
                        ErrorMessage = "Company not found"
                    });
                    result.FailureCount++;
                    continue;
                }

                // Store company info for email
                var companyName = company.Name;
                var contactEmail = company.ContactEmail;

                // ⭐ CASCADE 1: Soft delete all subscriptions (and their histories)
                await _repository.SoftDeleteSubscriptionsAsync(decryptedId);

                // ⭐ CASCADE 2: Soft delete all client tokens
                await _repository.SoftDeleteClientTokensAsync(decryptedId);

                // ⭐ CASCADE 3: Soft delete the company itself
                await _repository.DeleteAsync(decryptedId);
                result.SuccessfulIds.Add(encryptedId);
                result.SuccessCount++;

                // Send deletion email if requested
                if (request.SendEmailNotifications)
                {
                    try
                    {
                        await _emailService.SendCompanyDeletedEmailAsync(contactEmail, companyName, null); // Uses request culture
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send deletion email for {companyName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkOperationError
                {
                    CompanyId = encryptedId,
                    CompanyName = "Unknown",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BulkOperationResult> BulkActivateCompaniesAsync(BulkCompanyActionRequest request)
    {
        var result = new BulkOperationResult
        {
            TotalProcessed = request.CompanyIds.Count
        };

        foreach (var encryptedId in request.CompanyIds)
        {
            try
            {
                // Decrypt the ID
                var decryptedId = _mapper.Map<Guid>(new { CompanyId = encryptedId });
                
                var company = await _repository.GetByIdAsync(decryptedId, null);
                if (company == null)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        CompanyId = encryptedId,
                        CompanyName = "Unknown",
                        ErrorMessage = "Company not found"
                    });
                    result.FailureCount++;
                    continue;
                }

                company.IsActive = true;
                await _repository.UpdateAsync(company);
                result.SuccessfulIds.Add(encryptedId);
                result.SuccessCount++;

                // Send activation email if requested
                if (request.SendEmailNotifications)
                {
                    try
                    {
                        await _emailService.SendCompanyActivatedEmailAsync(company.ContactEmail, company.Name, null); // Uses request culture
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send activation email for {company.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkOperationError
                {
                    CompanyId = encryptedId,
                    CompanyName = "Unknown",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }

    public async Task<BulkOperationResult> BulkDeactivateCompaniesAsync(BulkCompanyActionRequest request)
    {
        var result = new BulkOperationResult
        {
            TotalProcessed = request.CompanyIds.Count
        };

        foreach (var encryptedId in request.CompanyIds)
        {
            try
            {
                // Decrypt the ID
                var decryptedId = _mapper.Map<Guid>(new { CompanyId = encryptedId });
                
                var company = await _repository.GetByIdAsync(decryptedId, null);
                if (company == null)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        CompanyId = encryptedId,
                        CompanyName = "Unknown",
                        ErrorMessage = "Company not found"
                    });
                    result.FailureCount++;
                    continue;
                }

                company.IsActive = false;
                await _repository.UpdateAsync(company);
                result.SuccessfulIds.Add(encryptedId);
                result.SuccessCount++;

                // Send deactivation email if requested
                if (request.SendEmailNotifications)
                {
                    try
                    {
                        await _emailService.SendCompanyDeactivatedEmailAsync(
                            company.ContactEmail, 
                            company.Name, 
                            request.Reason,
                            null); // Uses request culture
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send deactivation email for {company.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new BulkOperationError
                {
                    CompanyId = encryptedId,
                    CompanyName = "Unknown",
                    ErrorMessage = ex.Message
                });
                result.FailureCount++;
            }
        }

        if (result.SuccessCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        return result;
    }
}

