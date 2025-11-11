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

public class MenuItemsService : IMenuItemsService
{
    private readonly IMenuItemsRepository _repository;
    private readonly IMapper _mapper;
    private readonly ILocalizationService _localizer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public MenuItemsService(
        IMenuItemsRepository repository,
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

    public async Task<MenuItemssResponseDto> GetMenuItemssAsync()
    {
        var MenuItems = await _repository.GetActiveMenuItemssWithChildrenAsync();
        var MenuItemsList = MenuItems.ToList();

        // Get current user's admin type
        var currentUserType = _currentUserService.AdminTypeName;

        // Filter menu items based on AllowedUserTypes
        MenuItemsList = MenuItemsList
            .Where(m => CanUserSeeMenuItems(m, currentUserType))
            .ToList();

        // Map to DTOs
        var MenuItemsDtos = new List<MenuItemsDto>();
        foreach (var item in MenuItemsList)
        {
            var dto = MapMenuItemsWithChildren(item, currentUserType);
            MenuItemsDtos.Add(dto);
        }

        // Extract all unique pages from menu items
        var pages = ExtractPages(MenuItemsList);

        return new MenuItemssResponseDto
        {
            MenuItems = MenuItemsDtos,
            Pages = pages
        };
    }

    public async Task<MenuItemsDto?> GetMenuItemsByIdAsync(Guid id)
    {
        var MenuItems = await _repository.GetByIdAsync(id, new[] { "Children", "ParentMenuItems" });
        if (MenuItems == null || MenuItems.IsDeleted) return null;

        return MapMenuItemsWithChildren(MenuItems, _currentUserService.AdminTypeName);
    }

    public async Task<MenuItemsDto> CreateMenuItemsAsync(CreateMenuItemsDto dto)
    {
        var MenuItems = _mapper.Map<MenuItems>(dto);

        // Parent menu item ID is handled by AutoMapper via DecryptNullableGuidConverter
        // No manual decryption needed here

        var created = await _repository.AddAsync(MenuItems);
        await _unitOfWork.SaveChangesAsync();

        return MapMenuItemsWithChildren(created, _currentUserService.AdminTypeName);
    }

    public async Task<MenuItemsDto?> UpdateMenuItemsAsync(Guid id, UpdateMenuItemsDto dto)
    {
        var MenuItems = await _repository.GetByIdAsync(id, new[] { "Children", "ParentMenuItems" });
        if (MenuItems == null || MenuItems.IsDeleted)
        {
            throw new NotFoundException(_localizer["MenuItems.NotFound"]);
        }

        // Parent menu item ID validation and circular reference prevention
        if (dto.ParentMenuItemsId.HasValue)
        {
            if (dto.ParentMenuItemsId.Value == id)
            {
                throw new BadRequestException(_localizer["MenuItems.CircularReference"]);
            }

            // Check if the parent is a descendant (would create circular reference)
            if (await IsDescendantAsync(MenuItems, dto.ParentMenuItemsId.Value))
            {
                throw new BadRequestException(_localizer["MenuItems.CircularReference"]);
            }

            var parent = await _repository.GetByIdAsync(dto.ParentMenuItemsId.Value, null);
            if (parent == null || parent.IsDeleted)
            {
                throw new BadRequestException(_localizer["MenuItems.ParentNotFound"]);
            }
        }

        _mapper.Map(dto, MenuItems);
        await _repository.UpdateAsync(MenuItems);
        await _unitOfWork.SaveChangesAsync();

        // Reload with children to get full hierarchy
        var updated = await _repository.GetByIdAsync(id, new[] { "Children", "ParentMenuItems" });
        if (updated == null)
        {
            throw new NotFoundException(_localizer["MenuItems.NotFound"]);
        }

        return MapMenuItemsWithChildren(updated, _currentUserService.AdminTypeName);
    }

    public async Task<bool> DeleteMenuItemsAsync(Guid id)
    {
        var MenuItems = await _repository.GetByIdAsync(id, new[] { "Children" });
        if (MenuItems == null || MenuItems.IsDeleted)
        {
            throw new NotFoundException(_localizer["MenuItems.NotFound"]);
        }

        // Check if menu item has children
        if (MenuItems.Children.Any(c => !c.IsDeleted))
        {
            throw new BadRequestException(_localizer["MenuItems.HasChildren"]);
        }

        await _repository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Maps a menu item with its children recursively.
    /// </summary>
    private MenuItemsDto MapMenuItemsWithChildren(MenuItems item, string? currentUserType)
    {
        var dto = _mapper.Map<MenuItemsDto>(item);
        
        // Map children recursively
        if (item.Children != null && item.Children.Any())
        {
            var children = item.Children
                .Where(c => !c.IsDeleted && c.IsActive && CanUserSeeMenuItems(c, currentUserType));
            
            dto.Children = children
                .OrderBy(c => c.Order)
                .Select(c => MapMenuItemsWithChildren(c, currentUserType))
                .ToList();
        }

        // Map parent reference if exists
        if (item.ParentMenuItems != null)
        {
            dto.ParentMenuItems = _mapper.Map<MenuItemsReferenceDto>(item.ParentMenuItems);
        }

        return dto;
    }

    /// <summary>
    /// Checks if a user with the given admin type can see the menu item.
    /// If AllowedUserTypes is null or empty, the menu item is visible to all authenticated users.
    /// </summary>
    private bool CanUserSeeMenuItems(MenuItems item, string? currentUserType)
    {
        // If no allowed user types specified, visible to all
        if (string.IsNullOrEmpty(item.AllowedUserTypes))
        {
            return true;
        }

        // If user type is not specified, they can't see restricted items
        if (string.IsNullOrEmpty(currentUserType))
        {
            return false;
        }

        // Parse allowed user types from JSON
        try
        {
            var allowedTypes = JsonSerializer.Deserialize<List<string>>(item.AllowedUserTypes);
            if (allowedTypes == null || allowedTypes.Count == 0)
            {
                return true; // Empty list means visible to all
            }

            // Check if current user type is in the allowed list (case-insensitive)
            return allowedTypes.Any(type => 
                string.Equals(type, currentUserType, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            // If JSON parsing fails, default to visible to all (fail open)
            return true;
        }
    }

    /// <summary>
    /// Extracts all unique pages (routes) from menu items recursively.
    /// </summary>
    private List<string> ExtractPages(IEnumerable<MenuItems> items)
    {
        var pages = new HashSet<string>();

        foreach (var item in items)
        {
            ExtractPagesRecursive(item, pages);
        }

        return pages.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Recursively extracts pages from a menu item and its children.
    /// </summary>
    private void ExtractPagesRecursive(MenuItems item, HashSet<string> pages)
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

    /// <summary>
    /// Checks if a menu item is a descendant of another (to prevent circular references).
    /// </summary>
    private async Task<bool> IsDescendantAsync(MenuItems item, Guid potentialAncestorId)
    {
        if (item.ParentMenuItemsId == null)
        {
            return false;
        }

        var visited = new HashSet<Guid> { item.Id };
        var currentId = item.ParentMenuItemsId;

        while (currentId.HasValue)
        {
            if (currentId.Value == potentialAncestorId)
            {
                return true;
            }

            if (visited.Contains(currentId.Value))
            {
                // Circular reference detected
                break;
            }

            visited.Add(currentId.Value);

            var parent = await _repository.GetByIdAsync(currentId.Value, null);
            if (parent == null || parent.IsDeleted)
            {
                break;
            }

            currentId = parent.ParentMenuItemsId;
        }

        return false;
    }
}

