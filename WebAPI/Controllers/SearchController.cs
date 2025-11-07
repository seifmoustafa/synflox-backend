using Application.DTOs.Search;
using Application.Services_Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Controller for global search functionality across all entities
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        /// <summary>
        /// Performs a global search across all entities
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <returns>Search results with pagination and metadata</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromBody] SearchRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred during search for query: {request.Query}, Error: {ex.Message}");
                return StatusCode(500, new { error = "SEARCH_ERROR", message = "An error occurred during search" });
            }
        }

        /// <summary>
        /// Performs a simple search using query parameters (for easier frontend integration)
        /// </summary>
        /// <param name="query">Search query string</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="entityTypes">Comma-separated list of entity types to search</param>
        /// <param name="categoryIds">Comma-separated list of category IDs to filter by</param>
        /// <param name="includeInactive">Include inactive/deleted items</param>
        /// <param name="englishOnly">Search in English content only</param>
        /// <param name="arabicOnly">Search in Arabic content only</param>
        /// <returns>Search results</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SearchSimple(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? entityTypes = null,
            [FromQuery] string? categoryIds = null,
            [FromQuery] bool includeInactive = false,
            [FromQuery] bool englishOnly = false,
            [FromQuery] bool arabicOnly = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return BadRequest(new { error = "INVALID_QUERY", message = "Query must be at least 2 characters long" });
                }

                var request = new SearchRequestDto
                {
                    Query = query.Trim(),
                    Page = page,
                    PageSize = Math.Min(pageSize, 100), // Cap at 100
                    EntityTypes = !string.IsNullOrWhiteSpace(entityTypes) 
                        ? entityTypes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList()
                        : null,
                    CategoryIds = !string.IsNullOrWhiteSpace(categoryIds)
                        ? categoryIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Where(s => Guid.TryParse(s.Trim(), out _))
                            .Select(s => Guid.Parse(s.Trim()))
                            .ToList()
                        : null,
                    IncludeInactive = includeInactive,
                    EnglishOnly = englishOnly,
                    ArabicOnly = arabicOnly
                };

                var result = await _searchService.SearchAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred during simple search for query: {query}, Error: {ex.Message}");
                return StatusCode(500, new { error = "SEARCH_ERROR", message = "An error occurred during search" });
            }
        }

        /// <summary>
        /// Gets available entity types for filtering
        /// </summary>
        /// <returns>List of available entity type names</returns>
        [HttpGet("entity-types")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEntityTypes()
        {
            try
            {
                var entityTypes = await _searchService.GetAvailableEntityTypesAsync();
                return Ok(new { entityTypes });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while getting entity types, Error: {ex.Message}");
                return StatusCode(500, new { error = "ENTITY_TYPES_ERROR", message = "An error occurred while getting entity types" });
            }
        }

        /// <summary>
        /// Gets search suggestions based on partial query
        /// </summary>
        /// <param name="query">Partial search query</param>
        /// <param name="limit">Maximum number of suggestions</param>
        /// <returns>List of search suggestions</returns>
        [HttpGet("suggestions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSuggestions(
            [FromQuery] string query,
            [FromQuery] int limit = 10)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Ok(new { suggestions = new List<string>() });
                }

                var suggestions = await _searchService.GetSearchSuggestionsAsync(query.Trim(), Math.Min(limit, 50));
                return Ok(new { suggestions });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while getting search suggestions for query: {query}, Error: {ex.Message}");
                return StatusCode(500, new { error = "SUGGESTIONS_ERROR", message = "An error occurred while getting suggestions" });
            }
        }

        /// <summary>
        /// Gets search statistics and popular searches
        /// </summary>
        /// <returns>Search statistics</returns>
        [HttpGet("stats")]
        [AllowAnonymous]
        public async Task<IActionResult> GetSearchStats()
        {
            try
            {
                // This could be expanded to include actual search statistics
                // For now, return basic information
                var entityTypes = await _searchService.GetAvailableEntityTypesAsync();
                
                return Ok(new
                {
                    totalEntityTypes = entityTypes.Count,
                    availableEntityTypes = entityTypes,
                    message = "Search statistics feature coming soon"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred while getting search statistics, Error: {ex.Message}");
                return StatusCode(500, new { error = "STATS_ERROR", message = "An error occurred while getting search statistics" });
            }
        }
    }
}
