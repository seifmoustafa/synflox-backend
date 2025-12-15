using AutoMapper;
using Domain.Entities.Navigation;
using Application.DTOs.MenuItems;
using Application.Services;
using System.Linq;
using System.Text.Json;

namespace Application.Mapping;

public class MenuItemMappingProfile : Profile
{
    public MenuItemMappingProfile()
    {
        // ===== Admin Menu Item Mappings =====
        CreateMap<AdminMenuItem, AdminMenuItemDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children.OrderBy(c => c.Order)))
            .ForMember(d => d.Parent, opt => opt.MapFrom(s => s.Parent))
            .ForMember(d => d.AllowedUserTypes, opt => opt.MapFrom(s => 
                string.IsNullOrEmpty(s.AllowedUserTypes) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(s.AllowedUserTypes, (JsonSerializerOptions)null!) ?? new List<string>()));

        CreateMap<AdminMenuItem, AdminMenuItemReferenceDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));

        CreateMap<CreateAdminMenuItemDto, AdminMenuItem>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.ParentId, 
                opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid?>(s => s.ParentId))
            .ForMember(d => d.Parent, opt => opt.Ignore())
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

        CreateMap<UpdateAdminMenuItemDto, AdminMenuItem>()
            .ForMember(d => d.ParentId, 
                opt => opt.ConvertUsing<UniversalDecryptionConverter, Guid?>(s => s.ParentId))
            .ForMember(d => d.Parent, opt => opt.Ignore())
            .ForMember(d => d.Children, opt => opt.Ignore())
            .ForMember(d => d.AllowedUserTypes, opt => opt.MapFrom((src, dest) => 
                src.AllowedUserTypes == null 
                    ? dest.AllowedUserTypes 
                    : (src.AllowedUserTypes.Count == 0 ? null : JsonSerializer.Serialize(src.AllowedUserTypes, (JsonSerializerOptions)null!))))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // ===== Client Menu Item Mappings =====
        CreateMap<ClientMenuItem, ClientMenuItemDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id))
            .ForMember(d => d.Children, opt => opt.MapFrom(s => s.Children.OrderBy(c => c.Order)))
            .ForMember(d => d.Parent, opt => opt.MapFrom(s => s.Parent))
            .ForMember(d => d.RequiredPermissions, opt => opt.MapFrom(s => 
                string.IsNullOrEmpty(s.RequiredPermissions) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(s.RequiredPermissions, (JsonSerializerOptions)null!) ?? new List<string>()));

        CreateMap<ClientMenuItem, ClientMenuItemReferenceDto>()
            .ForMember(d => d.Id,
                opt => opt.ConvertUsing<UniversalEncryptionConverter, Guid>(s => s.Id));

        // ===== Request DTO Mappings - decrypt MenuItemId =====
        CreateMap<GetMenuItemByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<UpdateMenuItemByIdRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();

        CreateMap<DeleteMenuItemRequest, Guid>()
            .ConvertUsing<UniversalDecryptionConverter>();
    }
}

