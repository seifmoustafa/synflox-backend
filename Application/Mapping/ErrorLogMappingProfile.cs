using Application.DTOs.Logging;
using Application.Services;
using AutoMapper;
using Domain.Entities.Logging;

namespace Application.Mapping;

public class ErrorLogMappingProfile : Profile
{
    public ErrorLogMappingProfile()
    {
        CreateMap<ErrorLog, ErrorLogDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.UserId,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.UserId))
            .ForMember(d => d.CompanyId,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.CompanyId));
    }
}



