using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Licensing;
using Application.Services;
using AutoMapper;
using Domain.Entities.Common;
using Domain.Entities.Licensing;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

public class CompanyGroupService : ICompanyGroupService
{
    private readonly ICompanyGroupRepository _repository;
    private readonly ICompanyGroupMemberRepository _memberRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ILicensingService _licensingService;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyGroupService(
        ICompanyGroupRepository repository,
        ICompanyGroupMemberRepository memberRepository,
        ICompanyRepository companyRepository,
        ILicensingService licensingService,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _memberRepository = memberRepository;
        _companyRepository = companyRepository;
        _licensingService = licensingService;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<CompanyGroupDto> CreateGroupAsync(CreateCompanyGroupDto request)
    {
        var group = _mapper.Map<CompanyGroup>(request);
        var created = await _repository.AddAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyGroupDto>(created);
    }

    public async Task<(
        IEnumerable<CompanyGroupDto> Groups,
        PaginationMetadata Meta
    )> GetAllGroupsAsync(int page = 1, int pageSize = 10)
    {
        var (entities, meta) = await _repository.GetAllAsync(null, page, pageSize, null, default);
        var entitiesList = entities.ToList();

        // Get company counts for all groups in batch (before mapping to preserve entity IDs)
        var groupIds = entitiesList.Select(e => e.Id).ToList();
        var counts = await _repository.GetCompanyCountsForGroupsAsync(groupIds);

        // Map entities to DTOs
        var dtos = _mapper.Map<IEnumerable<CompanyGroupDto>>(entitiesList).ToList();

        // Set counts for each DTO by matching with entity by index (order is preserved)
        for (int i = 0; i < entitiesList.Count && i < dtos.Count; i++)
        {
            var entityId = entitiesList[i].Id;
            if (counts.TryGetValue(entityId, out var count))
            {
                dtos[i].CompaniesCount = count;
            }
        }

        return (dtos, meta);
    }

    public async Task<CompanyGroupDto?> GetGroupByIdAsync(Guid groupId)
    {
        var group = await _repository.GetByIdAsync(groupId, null);
        if (group == null || group.IsDeleted)
            return null;

        var dto = _mapper.Map<CompanyGroupDto>(group);

        // Get company count for this group
        dto.CompaniesCount = await _repository.GetCompanyCountInGroupAsync(groupId);

        return dto;
    }

    public async Task<CompanyGroupDto> UpdateGroupAsync(Guid groupId, UpdateCompanyGroupDto request)
    {
        var group = await _repository.GetByIdAsync(groupId, null);
        if (group == null || group.IsDeleted)
            throw new NotFoundException(_localizer["CompanyGroup.NotFound"]);

        _mapper.Map(request, group);
        await _repository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<CompanyGroupDto>(group);
    }

    public async Task DeleteGroupAsync(Guid groupId)
    {
        var group = await _repository.GetByIdAsync(groupId, null);
        if (group == null || group.IsDeleted)
            throw new NotFoundException(_localizer["CompanyGroup.NotFound"]);

        // Remove all companies from group first
        await _memberRepository.RemoveAllCompaniesFromGroupAsync(groupId);

        await _repository.DeleteAsync(groupId);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AddCompaniesToGroupAsync(Guid groupId, IEnumerable<Guid> companyIds)
    {
        var group = await _repository.GetByIdAsync(groupId, null);
        if (group == null || group.IsDeleted)
            throw new NotFoundException(_localizer["CompanyGroup.NotFound"]);

        foreach (var companyId in companyIds)
        {
            // Check if already in group
            if (await _memberRepository.IsCompanyInGroupAsync(companyId, groupId))
                continue;

            var member = new CompanyGroupMember
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                CompanyGroupId = groupId,
                IsActive = true,
                IsDeleted = false,
            };

            await _memberRepository.AddAsync(member);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveCompaniesFromGroupAsync(Guid groupId, IEnumerable<Guid> companyIds)
    {
        foreach (var companyId in companyIds)
        {
            await _memberRepository.RemoveCompanyFromGroupAsync(companyId, groupId);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<(
        IEnumerable<CompanyDto> Companies,
        PaginationMetadata Meta
    )> GetCompaniesInGroupAsync(Guid groupId, int page = 1, int pageSize = 10)
    {
        var (companies, totalCount) = await _repository.GetCompaniesInGroupAsync(
            groupId,
            page,
            pageSize
        );
        var dtos = _mapper.Map<IEnumerable<CompanyDto>>(companies);
        var meta = new PaginationMetadata(totalCount, pageSize, page);

        return (dtos, meta);
    }

    public async Task<IEnumerable<CompanyGroupDto>> GetGroupsForCompanyAsync(Guid companyId)
    {
        var groups = await _repository.GetGroupsForCompanyAsync(companyId);
        return _mapper.Map<IEnumerable<CompanyGroupDto>>(groups);
    }

    public async Task BulkActivateByGroupAsync(Guid groupId)
    {
        var companies = await _repository.GetAllCompaniesInGroupAsync(groupId);
        foreach (var company in companies)
        {
            await _licensingService.ActivateCompanyAsync(company.Id, DateTime.UtcNow.AddYears(1));
        }
    }

    public async Task BulkSuspendByGroupAsync(Guid groupId)
    {
        var companies = await _repository.GetAllCompaniesInGroupAsync(groupId);
        foreach (var company in companies)
        {
            await _licensingService.SuspendCompanyAsync(company.Id);
        }
    }

    public async Task BulkResumeByGroupAsync(Guid groupId)
    {
        var companies = await _repository.GetAllCompaniesInGroupAsync(groupId);
        foreach (var company in companies)
        {
            await _licensingService.ResumeCompanyAsync(company.Id);
        }
    }

    public async Task BulkExtendByGroupAsync(Guid groupId, DateTime expiryDate)
    {
        var companies = await _repository.GetAllCompaniesInGroupAsync(groupId);
        foreach (var company in companies)
        {
            await _licensingService.ExtendCompanyAsync(company.Id, expiryDate);
        }
    }
}
