using Application.DTOs.Search;

namespace Application.Services_Interfaces
{
    /// <summary>
    /// Service interface for global search functionality
    /// </summary>
    public interface ISearchService
    {
        /// <summary>
        /// Performs a global search across all entities
        /// </summary>
        /// <param name="request">Search request parameters</param>
        /// <returns>Search results with pagination and metadata</returns>
        Task<SearchResponseDto> SearchAsync(SearchRequestDto request);

        /// <summary>
        /// Gets available entity types for filtering
        /// </summary>
        /// <returns>List of available entity type names</returns>
        Task<List<string>> GetAvailableEntityTypesAsync();

        /// <summary>
        /// Gets search suggestions based on partial query
        /// </summary>
        /// <param name="query">Partial search query</param>
        /// <param name="limit">Maximum number of suggestions</param>
        /// <returns>List of search suggestions</returns>
        Task<List<string>> GetSearchSuggestionsAsync(string query, int limit = 10);
    }
}
