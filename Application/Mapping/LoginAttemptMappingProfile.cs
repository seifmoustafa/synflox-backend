using AutoMapper;
using Domain.Entities.Authentication;
using Application.DTOs.Authentication;
using Application.Services;

namespace Application.Mapping;

public class LoginAttemptMappingProfile : Profile
{
    public LoginAttemptMappingProfile()
    {
        CreateMap<LoginAttempt, LoginAttemptDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id))
            .ForMember(d => d.AdminId,
                opt => opt.ConvertUsing<EncryptNullableGuidConverter, Guid?>(s => s.AdminId));
    }
}

