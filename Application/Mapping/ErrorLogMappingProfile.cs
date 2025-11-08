using Application.DTOs.Logging;
using AutoMapper;
using Domain.Entities.Logging;

namespace Application.Mapping;

public class ErrorLogMappingProfile : Profile
{
    public ErrorLogMappingProfile()
    {
        CreateMap<ErrorLog, ErrorLogDto>();
    }
}



