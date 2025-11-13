using Application.DTOs.ClientAccess;
using AutoMapper;
using Domain.Entities.ClientAccess;
using System.Text.Json;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for client token entities and DTOs
/// </summary>
public class ClientTokenMappingProfile : Profile
{
    public ClientTokenMappingProfile()
    {
        // ClientAccessToken mappings
        CreateMap<ClientAccessToken, ClientTokenDto>()
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Company.Name))
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Subscription.Plan.Name))
            .ForMember(dest => dest.AllowedEndpoints, opt => opt.MapFrom(src => 
                ConvertAllowedEndpoints(src.AllowedEndpoints)))
            .ForMember(dest => dest.IsValid, opt => opt.MapFrom(src => src.IsValid))
            .ForMember(dest => dest.IsExpired, opt => opt.MapFrom(src => src.IsExpired))
            .ForMember(dest => dest.DaysUntilExpiry, opt => opt.MapFrom(src => src.DaysUntilExpiry));

        // ClientTokenUsageLog mappings
        CreateMap<ClientTokenUsageLog, ClientTokenUsageLogDto>()
            .ForMember(dest => dest.IsSuccessful, opt => opt.MapFrom(src => src.IsSuccessful));

        // Reverse mappings for create/update operations
        CreateMap<GenerateClientTokenRequest, ClientAccessToken>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.TokenHash, opt => opt.Ignore())
            .ForMember(dest => dest.IssuedAtUtc, opt => opt.Ignore())
            .ForMember(dest => dest.ExpiresAtUtc, opt => opt.MapFrom(src => src.CustomExpiryDate))
            .ForMember(dest => dest.AllowedEndpoints, opt => opt.MapFrom(src => 
                ConvertEndpointsToJson(src.AllowedEndpoints)))
            .ForMember(dest => dest.CompanyId, opt => opt.Ignore())
            .ForMember(dest => dest.SubscriptionId, opt => opt.MapFrom(src => src.SubscriptionId))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.ClientTokenStatus.Active))
            .ForMember(dest => dest.UsageCount, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.TokenVersion, opt => opt.MapFrom(src => "1.0"));

        // Usage log creation mapping
        CreateMap<ClientTokenUsageLog, ClientTokenUsageLogDto>();
        
        // ClientAccessToken to GenerateClientTokenResponse mapping
        CreateMap<ClientAccessToken, GenerateClientTokenResponse>()
            .ForMember(dest => dest.AccessToken, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.TokenId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.IssuedAtUtc, opt => opt.MapFrom(src => src.IssuedAtUtc))
            .ForMember(dest => dest.ExpiresAtUtc, opt => opt.MapFrom(src => src.ExpiresAtUtc))
            .ForMember(dest => dest.AllowedEndpoints, opt => opt.MapFrom(src => 
                ConvertAllowedEndpoints(src.AllowedEndpoints)))
            .ForMember(dest => dest.TokenVersion, opt => opt.MapFrom(src => src.TokenVersion))
            .ForMember(dest => dest.CompanyId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(src => src.CompanyId))
            .ForMember(dest => dest.CompanyName, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.SubscriptionId, opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(src => src.SubscriptionId))
            .ForMember(dest => dest.PlanName, opt => opt.Ignore()) // Set manually in service
            .ForMember(dest => dest.Usage, opt => opt.MapFrom(src => "Include this token in the Authorization header as 'Bearer {token}' when making API calls."))
            .ForMember(dest => dest.SecurityWarning, opt => opt.MapFrom(src => "This token will only be shown once. Store it securely and never share it."));
        
        // Request DTOs for ID decryption
        CreateMap<SubscriptionIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
        CreateMap<CompanyIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }

    private static List<string> ConvertAllowedEndpoints(string? allowedEndpoints)
    {
        if (string.IsNullOrEmpty(allowedEndpoints))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(allowedEndpoints) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string? ConvertEndpointsToJson(IEnumerable<Domain.Enums.ClientEndpointType>? endpoints)
    {
        if (endpoints == null || !endpoints.Any())
            return null;

        try
        {
            return JsonSerializer.Serialize(endpoints.Select(e => e.ToString()).ToList());
        }
        catch
        {
            return null;
        }
    }
}
