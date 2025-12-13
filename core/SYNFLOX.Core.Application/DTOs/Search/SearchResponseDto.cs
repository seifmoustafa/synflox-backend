using Application.DTOs.Responses;

namespace Application.DTOs.Search
{
    /// <summary>
    /// Response DTO for global search results
    /// </summary>
    public class SearchResponseDto
    {
        /// <summary>
        /// Search results grouped by entity type
        /// </summary>
        public Dictionary<string, List<SearchResultDto>> ResultsByType { get; set; } = new();

        /// <summary>
        /// All search results combined
        /// </summary>
        public List<SearchResultDto> AllResults { get; set; } = new();

        /// <summary>
        /// Pagination information
        /// </summary>
        public PaginationDto Pagination { get; set; } = null!;

        /// <summary>
        /// Search metadata
        /// </summary>
        public SearchMetadataDto Metadata { get; set; } = null!;
    }

    /// <summary>
    /// Search operation metadata
    /// </summary>
    public class SearchMetadataDto
    {
        /// <summary>
        /// Original search query
        /// </summary>
        public string Query { get; set; } = null!;

        /// <summary>
        /// Total number of results found
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// Number of entity types that had results
        /// </summary>
        public int EntityTypesWithResults { get; set; }

        /// <summary>
        /// Search execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Entity types that were searched
        /// </summary>
        public List<string> SearchedEntityTypes { get; set; } = new();

        /// <summary>
        /// Entity types that had results
        /// </summary>
        public List<string> EntityTypesWithResultsList { get; set; } = new();
    }
}
