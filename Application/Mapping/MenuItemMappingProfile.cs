using AutoMapper;
using Domain.Entities.Navigation;
using Application.DTOs.MenuItems;
using System.Linq;
using System.Text.Json;

namespace Application.Mapping;

public class MenuItemsMappingProfile : Profile
{
    public MenuItemsMappingProfile()
    {
        CreateMap<MenuItems, MenuItemsDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidToStringConverter, Guid>(s => s.Id))
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children.OrderBy(c => c.Order)))
            .ForMember(d => d.ParentMenuItems, opt => opt.MapFrom(s => s.ParentMenuItems))
            .ForMember(d => d.AllowedUserTypes, opt => opt.MapFrom(s => 
                string.IsNullOrEmpty(s.AllowedUserTypes) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(s.AllowedUserTypes, (JsonSerializerOptions)null!) ?? new List<string>()));

        CreateMap<MenuItems, MenuItemsReferenceDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<EncryptGuidToStringConverter, Guid>(s => s.Id));

        CreateMap<CreateMenuItemsDto, MenuItems>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ParentMenuItemsId, opt => opt.Ignore()) // Will be set manually
            .ForMember(d => d.ParentMenuItems, opt => opt.Ignore())
            .ForMember(d => d.Children, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => true))
            .ForMember(d => d.AllowedUserTypes, opt => opt.MapFrom(s => 
                s.AllowedUserTypes == null || s.AllowedUserTypes.Count == 0 
                    ? null 
                    : JsonSerializer.Serialize(s.AllowedUserTypes, (JsonSerializerOptions)null!)))
            .ForMember(d => d.CreatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.UpdatedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.DeletedTimestamp, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore());

        CreateMap<UpdateMenuItemsDto, MenuItems>()
            .ForMember(d => d.ParentMenuItemsId, opt => opt.Ignore()) // Will be set manually
            .ForMember(d => d.ParentMenuItems, opt => opt.Ignore())
            .ForMember(d => d.Children, opt => opt.Ignore())
            .ForMember(d => d.AllowedUserTypes, opt => opt.MapFrom((src, dest) => 
                src.AllowedUserTypes == null 
                    ? dest.AllowedUserTypes 
                    : (src.AllowedUserTypes.Count == 0 ? null : JsonSerializer.Serialize(src.AllowedUserTypes, (JsonSerializerOptions)null!))))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}

