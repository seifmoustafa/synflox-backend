using AutoMapper;
using Domain.Entities.Reporting;
using Application.DTOs.Reporting;
using Application.Services;

namespace Application.Mapping;

public class ReportDefinitionMappingProfile : Profile
{
    public ReportDefinitionMappingProfile()
    {
        CreateMap<ReportDefinition, ReportDefinitionDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidConverter, Guid>(s => s.Id));
    }
}

