using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Application.DTOs.MenuItems;
using Application.Services;
using AutoMapper;
using Domain.Entities.Navigation;
using Domain.Exceptions;
using Domain.Interfaces;

namespace Infrastructure.Services;

/// <summary>
/// Service for managing admin portal menu items.
/// </summary>
public class AdminMenuItemService : IAdminMenuItemService
{
    private readonly IAdminMenuItemRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public AdminMenuItemService(
        IAdminMenuItemRepository repository,
        IMapper mapper,
        ILocalizationService localizer,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _mapper = mapper;
        _localizer = localizer;
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<AdminMenuItemsResponseDto> GetMenuItemsAsync()
    {
        var menuItems = await _repository.GetActiveMenuItemsWithChildrenAsync();
        var menuItemsList = menuItems.ToList();

        // Get current user's admin type
        var currentUserType = _currentUserService.AdminTypeName;

        // Filter menu items based on AllowedUserTypes
        menuItemsList = menuItemsList
            .Where(m => CanUserSeeMenuItem(m, currentUserType))
            .ToList();

        // Map to DTOs
        var menuItemDtos = new List<AdminMenuItemDto>();
        foreach (var item in menuItemsList)
        {
            var dto = MapMenuItemWithChildren(item, currentUserType);
            menuItemDtos.Add(dto);
        }

        // Extract all unique pages from menu items
        var pages = ExtractPages(menuItemsList);

        return new AdminMenuItemsResponseDto
        {
            MenuItems = menuItemDtos,
            Pages = pages
        };
    }

    public async Task<AdminMenuItemDto?> GetMenuItemByIdAsync(GetMenuItemByIdRequest request)
    {
        var decryptedId = _mapper.Map<Guid>(request);
        
        var menuItem = await _repository.GetByIdAsync(decryptedId, new[] { "Children", "Parent" });
        if (menuItem == null || menuItem.IsDeleted) return null;

        return MapMenuItemWithChildren(menuItem, _currentUserService.AdminTypeName);
    }

    public async Task<AdminMenuItemDto> CreateMenuItemAsync(CreateAdminMenuItemDto dto)
    {
        var menuItem = _mapper.Map<AdminMenuItem>(dto);

        var created = await _repository.AddAsync(menuItem);
        await _unitOfWork.SaveChangesAsync();

        return MapMenuItemWithChildren(created, _currentUserService.AdminTypeName);
    }

    public async Task<AdminMenuItemDto?> UpdateMenuItemAsync(UpdateMenuItemByIdRequest request)
    {
        var decryptedId = _mapper.Map<Guid>(request);
        
        var menuItem = await _repository.GetByIdAsync(decryptedId, new[] { "Children", "Parent" });
        if (menuItem == null || menuItem.IsDeleted)
        {
            throw new NotFoundException(_localizer["MenuItem.NotFound"]);
        }

        // Parent menu item ID validation and circular reference prevention
        if (request.UpdateData.ParentId.HasValue)
        {
            if (request.UpdateData.ParentId.Value == decryptedId)
            {
                throw new BadRequestException(_localizer["MenuItem.CircularReference"]);
            }

            if (await IsDescendantAsync(menuItem, request.UpdateData.ParentId.Value))
            {
                throw new BadRequestException(_localizer["MenuItem.CircularReference"]);
            }

            var parent = await _repository.GetByIdAsync(request.UpdateData.ParentId.Value, null);
            if (parent == null || parent.IsDeleted)
            {
                throw new BadRequestException(_localizer["MenuItem.ParentNotFound"]);
            }
        }

        _mapper.Map(request.UpdateData, menuItem);
        await _repository.UpdateAsync(menuItem);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(decryptedId, new[] { "Children", "Parent" });
        if (updated == null)
        {
            throw new NotFoundException(_localizer["MenuItem.NotFound"]);
        }

        return MapMenuItemWithChildren(updated, _currentUserService.AdminTypeName);
    }

    public async Task<bool> DeleteMenuItemAsync(DeleteMenuItemRequest request)
    {
        var decryptedId = _mapper.Map<Guid>(request);
        
        var menuItem = await _repository.GetByIdAsync(decryptedId, new[] { "Children" });
        if (menuItem == null || menuItem.IsDeleted)
        {
            throw new NotFoundException(_localizer["MenuItem.NotFound"]);
        }

        if (menuItem.Children.Any(c => !c.IsDeleted))
        {
            throw new BadRequestException(_localizer["MenuItem.HasChildren"]);
        }

        await _repository.DeleteAsync(decryptedId);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    private AdminMenuItemDto MapMenuItemWithChildren(AdminMenuItem item, string? currentUserType)
    {
        var dto = _mapper.Map<AdminMenuItemDto>(item);
        
        if (item.Children != null && item.Children.Any())
        {
            var children = item.Children
                .Where(c => !c.IsDeleted && c.IsActive && CanUserSeeMenuItem(c, currentUserType));
            
            dto.Children = children
                .OrderBy(c => c.Order)
                .Select(c => MapMenuItemWithChildren(c, currentUserType))
                .ToList();
        }

        if (item.Parent != null)
        {
            dto.Parent = _mapper.Map<AdminMenuItemReferenceDto>(item.Parent);
        }

        return dto;
    }

    private bool CanUserSeeMenuItem(AdminMenuItem item, string? currentUserType)
    {
        if (string.IsNullOrEmpty(item.AllowedUserTypes))
        {
            return true;
        }

        if (string.IsNullOrEmpty(currentUserType))
        {
            return false;
        }

        try
        {
            var allowedTypes = JsonSerializer.Deserialize<List<string>>(item.AllowedUserTypes);
            if (allowedTypes == null || allowedTypes.Count == 0)
            {
                return true;
            }

            return allowedTypes.Any(type => 
                string.Equals(type, currentUserType, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return true;
        }
    }

    private List<string> ExtractPages(IEnumerable<AdminMenuItem> items)
    {
        var pages = new HashSet<string>();

        foreach (var item in items)
        {
            ExtractPagesRecursive(item, pages);
        }

        return pages.OrderBy(p => p).ToList();
    }

    private void ExtractPagesRecursive(AdminMenuItem item, HashSet<string> pages)
    {
        if (!string.IsNullOrEmpty(item.Href))
        {
            pages.Add(item.Href);
        }

        if (item.Children != null)
        {
            foreach (var child in item.Children.Where(c => !c.IsDeleted && c.IsActive))
            {
                ExtractPagesRecursive(child, pages);
            }
        }
    }

    private async Task<bool> IsDescendantAsync(AdminMenuItem item, Guid potentialAncestorId)
    {
        if (item.ParentId == null)
        {
            return false;
        }

        var visited = new HashSet<Guid> { item.Id };
        var currentId = item.ParentId;

        while (currentId.HasValue)
        {
            if (currentId.Value == potentialAncestorId)
            {
                return true;
            }

            if (visited.Contains(currentId.Value))
            {
                break;
            }

            visited.Add(currentId.Value);

            var parent = await _repository.GetByIdAsync(currentId.Value, null);
            if (parent == null || parent.IsDeleted)
            {
                break;
            }

            currentId = parent.ParentId;
        }

        return false;
    }
}

/// <summary>
/// Service for managing client portal menu items.
/// </summary>
public class ClientMenuItemService : IClientMenuItemService
{
    private readonly IClientMenuItemRepository _repository;
    private readonly IMapper _mapper;

    public ClientMenuItemService(
        IClientMenuItemRepository repository,
        IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<ClientMenuItemsResponseDto> GetMenuItemsAsync(List<string>? permissions)
    {
        var menuItems = await _repository.GetActiveMenuItemsWithChildrenAsync();
        var menuItemsList = menuItems.ToList();

        // Filter menu items based on permissions
        menuItemsList = menuItemsList
            .Where(m => CanUserSeeMenuItem(m, permissions))
            .ToList();

        // Map to DTOs
        var menuItemDtos = new List<ClientMenuItemDto>();
        foreach (var item in menuItemsList)
        {
            var dto = MapMenuItemWithChildren(item, permissions);
            menuItemDtos.Add(dto);
        }

        // Extract all unique pages from menu items
        var pages = ExtractPages(menuItemsList);

        return new ClientMenuItemsResponseDto
        {
            MenuItems = menuItemDtos,
            Pages = pages
        };
    }

    public async Task<ClientMenuItemDto?> GetMenuItemByIdAsync(Guid id)
    {
        var menuItem = await _repository.GetByIdAsync(id, new[] { "Children", "Parent" });
        if (menuItem == null || menuItem.IsDeleted) return null;

        return MapMenuItemWithChildren(menuItem, null);
    }

    private ClientMenuItemDto MapMenuItemWithChildren(ClientMenuItem item, List<string>? permissions)
    {
        var dto = _mapper.Map<ClientMenuItemDto>(item);
        
        if (item.Children != null && item.Children.Any())
        {
            var children = item.Children
                .Where(c => !c.IsDeleted && c.IsActive && CanUserSeeMenuItem(c, permissions));
            
            dto.Children = children
                .OrderBy(c => c.Order)
                .Select(c => MapMenuItemWithChildren(c, permissions))
                .ToList();
        }

        if (item.Parent != null)
        {
            dto.Parent = _mapper.Map<ClientMenuItemReferenceDto>(item.Parent);
        }

        return dto;
    }

    private bool CanUserSeeMenuItem(ClientMenuItem item, List<string>? permissions)
    {
        // If no required permissions, visible to all
        if (string.IsNullOrEmpty(item.RequiredPermissions))
        {
            return true;
        }

        // If no permissions provided, user can't see restricted items
        if (permissions == null || permissions.Count == 0)
        {
            return false;
        }

        try
        {
            var requiredPermissions = JsonSerializer.Deserialize<List<string>>(item.RequiredPermissions);
            if (requiredPermissions == null || requiredPermissions.Count == 0)
            {
                return true;
            }

            // User must have at least one of the required permissions
            return requiredPermissions.Any(required => 
                permissions.Any(p => string.Equals(p, required, StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            return true;
        }
    }

    private List<string> ExtractPages(IEnumerable<ClientMenuItem> items)
    {
        var pages = new HashSet<string>();

        foreach (var item in items)
        {
            ExtractPagesRecursive(item, pages);
        }

        return pages.OrderBy(p => p).ToList();
    }

    private void ExtractPagesRecursive(ClientMenuItem item, HashSet<string> pages)
    {
        if (!string.IsNullOrEmpty(item.Href))
        {
            pages.Add(item.Href);
        }

        if (item.Children != null)
        {
            foreach (var child in item.Children.Where(c => !c.IsDeleted && c.IsActive))
            {
                ExtractPagesRecursive(child, pages);
            }
        }
    }
}

