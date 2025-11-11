using System;
using System.Threading.Tasks;
using Application.DTOs.MenuItems;
using Application.DTOs.Responses;
using Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenuItemsController : ControllerBase
{
    private readonly IMenuItemsService _MenuItemsService;
    private readonly ILocalizationService _localizer;

    public MenuItemsController(
        IMenuItemsService MenuItemsService,
        ILocalizationService localizer)
    {
        _MenuItemsService = MenuItemsService;
        _localizer = localizer;
    }

    /// <summary>
    /// Gets all active menu items with their children, ordered by Order field.
    /// Returns menu items and allowed pages array for navigation.
    /// System menu and its children are only visible to SuperAdmin.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true, Duration = 0)]
    public async Task<IActionResult> GetMenuItemss()
    {
        try
        {
            // Disable caching for this endpoint - response varies by user type
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";
            
            var result = await _MenuItemsService.GetMenuItemssAsync();
            return Ok(new ApiResponse<MenuItemssResponseDto>(200, _localizer["MenuItems.Success"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Gets a menu item by ID.
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> GetMenuItemsById(Guid id)
    {
        try
        {
            var request = new GetMenuItemByIdRequest { MenuItemId = id };
            var result = await _MenuItemsService.GetMenuItemsByIdAsync(request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["MenuItems.NotFound"]));
            }
            return Ok(new ApiResponse<MenuItemsDto>(200, _localizer["MenuItems.Success"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Creates a new menu item.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> CreateMenuItems([FromBody] CreateMenuItemsDto request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request == null) return BadRequest();

        try
        {
            var result = await _MenuItemsService.CreateMenuItemsAsync(request);
            return Ok(new ApiResponse<MenuItemsDto>(200, _localizer["MenuItems.Created"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing menu item.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> UpdateMenuItems(Guid id, [FromBody] UpdateMenuItemsDto updateData)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (updateData == null) return BadRequest();

        try
        {
            var request = new UpdateMenuItemByIdRequest { MenuItemId = id, UpdateData = updateData };
            var result = await _MenuItemsService.UpdateMenuItemsAsync(request);
            if (result == null)
            {
                return NotFound(new ApiResponse<string>(404, _localizer["MenuItems.NotFound"]));
            }
            return Ok(new ApiResponse<MenuItemsDto>(200, _localizer["MenuItems.Updated"], result));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }

    /// <summary>
    /// Deletes a menu item (soft delete).
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "SuperAdminOnly")]
    public async Task<IActionResult> DeleteMenuItems(Guid id)
    {
        try
        {
            var request = new DeleteMenuItemRequest { MenuItemId = id };
            await _MenuItemsService.DeleteMenuItemsAsync(request);
            return Ok(new ApiResponse<string>(200, _localizer["MenuItems.Deleted"], null));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<string>(400, ex.Message));
        }
    }
}

