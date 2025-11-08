using AutoMapper;
using Domain.Entities.Authentication;
using Application.DTOs.Authentication;
using Application.Services;
using System.Text.Json;

namespace Application.Mapping;

public class ApiKeyMappingProfile : Profile
{
    public ApiKeyMappingProfile()
    {
        CreateMap<ApiKey, ApiKeyDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.AllowedIps,
                opt => opt.MapFrom(s => !string.IsNullOrEmpty(s.AllowedIps)
                    ? JsonSerializer.Deserialize<string[]>(s.AllowedIps, new JsonSerializerOptions())
                    : null));

        CreateMap<CreateApiKeyRequest, ApiKey>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<DecryptGuidConverter, Guid>(s => s.CompanyId))
            .ForMember(d => d.KeyHash, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.KeyPrefix, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.SigningSecret, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.AllowedIps, opt => opt.MapFrom(s => 
                s.AllowedIps != null && s.AllowedIps.Length > 0
                    ? JsonSerializer.Serialize(s.AllowedIps, new JsonSerializerOptions())
                    : null))
            .ForMember(d => d.LastUsedAt, opt => opt.Ignore())
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateApiKeyRequest, ApiKey>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.CompanyId, opt => opt.Ignore())
            .ForMember(d => d.KeyHash, opt => opt.Ignore())
            .ForMember(d => d.KeyPrefix, opt => opt.Ignore())
            .ForMember(d => d.SigningSecret, opt => opt.Ignore())
            .ForMember(d => d.LastUsedAt, opt => opt.Ignore())
            .ForMember(d => d.AllowedIps, opt => opt.MapFrom(s => 
                s.AllowedIps != null && s.AllowedIps.Length > 0
                    ? JsonSerializer.Serialize(s.AllowedIps, new JsonSerializerOptions())
                    : null))
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Mapping for CreateApiKeyResponse - ID is encrypted via mapper
        CreateMap<ApiKey, CreateApiKeyResponse>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.ApiKey, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.KeyPrefix, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.SigningSecret, opt => opt.Ignore()) // Set manually in service
            .ForMember(d => d.Message, opt => opt.Ignore()); // Set manually in service
    }
}

