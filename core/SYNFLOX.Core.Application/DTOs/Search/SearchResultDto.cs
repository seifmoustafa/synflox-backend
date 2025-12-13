namespace Application.DTOs.Search
{
    /// <summary>
    /// Individual search result item
    /// </summary>
    public class SearchResultDto
    {
        /// <summary>
        /// Entity ID
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Entity type (e.g., "News", "Standard", "Article") - Use this for frontend navigation
        /// </summary>
        public string EntityType { get; set; } = null!;

        /// <summary>
        /// Generic hierarchy path for navigation (e.g., ["categoryId", "standardId", "controlId", "safeguardId", "techniqueId"])
        /// </summary>
        public List<Guid> HierarchyPath { get; set; } = new List<Guid>();

        /// <summary>
        /// Navigation route for frontend (e.g., "/news/{id}", "/standards/{standardId}/{controlId}/{safeguardId}/{techniqueId}/{implementationStepId}")
        /// </summary>
        public string NavigationRoute { get; set; } = null!;

        /// <summary>
        /// Primary title/name of the entity
        /// </summary>
        public string Title { get; set; } = null!;

        /// <summary>
        /// English title/name (if available)
        /// </summary>
        public string? TitleEn { get; set; }

        /// <summary>
        /// Content summary or description
        /// </summary>
        public string? Summary { get; set; }

        /// <summary>
        /// English summary or description (if available)
        /// </summary>
        public string? SummaryEn { get; set; }

        /// <summary>
        /// Category information (if applicable)
        /// </summary>
        public CategoryInfoDto? Category { get; set; }

        /// <summary>
        /// Image URL (if available)
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Creation date
        /// </summary>
        public DateTime CreatedTimestamp { get; set; }

        /// <summary>
        /// Relevance score (0-100)
        /// </summary>
        public int RelevanceScore { get; set; }

        /// <summary>
        /// Highlighted matching text snippets
        /// </summary>
        public List<string> Highlights { get; set; } = new List<string>();

        /// <summary>
        /// Tags (if available)
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

    }

    /// <summary>
    /// Category information for search results
    /// </summary>
    public class CategoryInfoDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? NameEn { get; set; }
        public string CategoryType { get; set; } = null!; // e.g., "NewsCategory", "StandardCategory"
    }
}
