using Application.DTOs.Tenancy;
using AutoMapper;
using Domain.Entities.Tenancy;

namespace Application.Mapping;

public class TenantMappingProfile : Profile
{
    public TenantMappingProfile()
    {
        CreateMap<Tenant, TenantDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));
    }
}


