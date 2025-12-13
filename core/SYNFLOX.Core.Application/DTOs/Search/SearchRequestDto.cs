using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Search
{
    /// <summary>
    /// Request DTO for global search across all entities
    /// </summary>
    public class SearchRequestDto
    {
        /// <summary>
        /// Search query string
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 2)]
        public string Query { get; set; } = null!;

        /// <summary>
        /// Page number (1-based)
        /// </summary>
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        [Range(1, 100)]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Filter by specific entity types (optional)
        /// If empty, searches all entity types
        /// </summary>
        public List<string>? EntityTypes { get; set; }

        /// <summary>
        /// Filter by specific categories (optional)
        /// </summary>
        public List<Guid>? CategoryIds { get; set; }

        /// <summary>
        /// Include inactive/deleted items in search results
        /// </summary>
        public bool IncludeInactive { get; set; } = false;

        /// <summary>
        /// Search in English content only
        /// </summary>
        public bool EnglishOnly { get; set; } = false;

        /// <summary>
        /// Search in Arabic content only
        /// </summary>
        public bool ArabicOnly { get; set; } = false;
    }
}
