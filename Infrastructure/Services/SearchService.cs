using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Application.DTOs.Search;
using Application.DTOs.Responses;
using Application.Services_Interfaces;
using Infrastructure.Context;

namespace Infrastructure.Services
{
    /// <summary>
    /// Completely generic search service that automatically discovers and searches all entities
    /// Ready for template use - no hardcoded entity names or attributes
    /// </summary>
    public class SearchService : ISearchService
    {
        private readonly ApplicationDBContext _context;

        public SearchService(ApplicationDBContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Main search method - automatically discovers all entities and searches them
        /// </summary>
        public async Task<SearchResponseDto> SearchAsync(SearchRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();
            var results = new List<SearchResultDto>();

            try
            {
                // Automatically discover ALL entities from DbContext
                var allEntityTypes = GetSearchableEntityTypes();

                // Filter entity types based on request
                var entityTypes = allEntityTypes.AsEnumerable();
                if (request.EntityTypes?.Any() == true)
                {
                    entityTypes = entityTypes.Where(et => request.EntityTypes.Contains(et, StringComparer.OrdinalIgnoreCase));
                }
                var entityTypesList = entityTypes.ToList();

                // Search all entities sequentially to avoid DbContext concurrency issues
                var allResults = new Dictionary<string, List<SearchResultDto>>();
                foreach (var entityType in entityTypesList)
                {
                    var result = await SearchEntityAsync(entityType, request);
                    allResults[entityType] = result;
                }

                // Combine all results
                results = allResults.Values.SelectMany(r => r).ToList();

                // Sort by relevance
                results = results.OrderByDescending(r => r.RelevanceScore).ToList();

                // Apply pagination
                var totalResults = results.Count;
                var paginatedResults = results
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                stopwatch.Stop();

                return new SearchResponseDto
                {
                    AllResults = paginatedResults,
                    ResultsByType = new Dictionary<string, List<SearchResultDto>>(),
                    Pagination = new PaginationDto(totalResults, request.PageSize, request.Page),
                    Metadata = new SearchMetadataDto
                    {
                        Query = request.Query,
                        TotalResults = totalResults,
                        EntityTypesWithResults = allResults.Count(kvp => kvp.Value.Any()),
                        ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                        SearchedEntityTypes = entityTypesList,
                        EntityTypesWithResultsList = allResults.Where(kvp => kvp.Value.Any()).Select(kvp => kvp.Key).ToList()
                    }
                };
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Get available entity types for search
        /// </summary>
        public Task<List<string>> GetAvailableEntityTypesAsync()
        {
            return Task.FromResult(GetSearchableEntityTypes());
        }

        /// <summary>
        /// Get search suggestions for autocomplete
        /// </summary>
        public async Task<List<string>> GetSearchSuggestionsAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return new List<string>();

            var suggestions = new List<string>();
            var entityTypes = GetSearchableEntityTypes();

            foreach (var entityType in entityTypes.Take(5)) // Limit to first 5 entity types for performance
            {
                try
                {
                    var entitySuggestions = await GetEntitySuggestionsAsync(entityType, query, limit);
                    suggestions.AddRange(entitySuggestions);
                }
                catch
                {
                    // Continue with other entities if one fails
                    continue;
                }
            }

            return suggestions
                .Distinct()
                .OrderBy(s => s)
                .Take(limit)
                .ToList();
        }

        #region Private Methods

        /// <summary>
        /// Automatically discover all searchable entity types from DbContext
        /// </summary>
        private List<string> GetSearchableEntityTypes()
        {
            var dbContextType = _context.GetType();
            var dbSetProperties = dbContextType.GetProperties()
                .Where(p => p.PropertyType.IsGenericType && 
                           p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToList();

            var entityTypes = new List<string>();
            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GetGenericArguments()[0];
                
                // Only include entities that have string properties for searching
                if (HasSearchableStringProperties(entityType))
                {
                    entityTypes.Add(entityType.Name);
                }
            }

            return entityTypes;
        }

        /// <summary>
        /// Check if entity type has searchable string properties
        /// </summary>
        private bool HasSearchableStringProperties(Type entityType)
        {
            var properties = entityType.GetProperties();
            var excludeProperties = new[] { "Id", "CreatedTimestamp", "UpdatedTimestamp", "DeletedTimestamp", "IsActive", "IsDeleted", "CreatedBy", "UpdatedBy", "DeletedBy" };
            
            return properties.Any(p => p.PropertyType == typeof(string) && 
                                      !excludeProperties.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Search a specific entity type
        /// </summary>
        private async Task<List<SearchResultDto>> SearchEntityAsync(string entityTypeName, SearchRequestDto request)
        {
            try
            {
                var dbSetProperty = GetDbSetProperty(entityTypeName);
                if (dbSetProperty == null) return new List<SearchResultDto>();

                var dbSet = dbSetProperty.GetValue(_context);
                if (dbSet == null) return new List<SearchResultDto>();

                // Get query with safe includes
                var query = GetQueryWithSafeIncludes(dbSet, entityTypeName);
                
                // Execute search
                var objectQuery = (IQueryable<object>)query;
                var allEntities = await objectQuery.ToListAsync();
                
                var filteredEntities = allEntities.AsEnumerable();
                
                // Apply filters
                filteredEntities = ApplyFilters(filteredEntities, request, entityTypeName);
                
                // Build results
                var results = filteredEntities.Select(e => BuildSearchResult(e, entityTypeName, request.Query))
                    .OrderByDescending(r => r.RelevanceScore)
                    .ToList();
                
                return results;
            }
            catch
            {
                return new List<SearchResultDto>();
            }
        }

        /// <summary>
        /// Apply search and other filters to entities
        /// </summary>
        private IEnumerable<object> ApplyFilters(IEnumerable<object> entities, SearchRequestDto request, string entityTypeName)
        {
            var filteredEntities = entities;

            // Active/Inactive filter
            if (!request.IncludeInactive)
            {
                filteredEntities = filteredEntities.Where(e => IsEntityActive(e));
            }

            // Search filter (case-insensitive)
            var searchQuery = request.Query;
            filteredEntities = filteredEntities.Where(e => ContainsSearchTerm(e, searchQuery, request));

            // Category filter
            if (request.CategoryIds?.Any() == true)
            {
                filteredEntities = filteredEntities.Where(e => 
                {
                    var categoryId = GetEntityCategoryId(e);
                    return categoryId.HasValue && request.CategoryIds.Contains(categoryId.Value);
                });
            }

            return filteredEntities;
        }

        /// <summary>
        /// Check if entity contains search term in any string property
        /// </summary>
        private bool ContainsSearchTerm(object entity, string searchQuery, SearchRequestDto request)
        {
            var properties = entity.GetType().GetProperties();
            
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(entity) as string;
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Apply language filter
                        var isArabicProperty = prop.Name.EndsWith("Ar", StringComparison.OrdinalIgnoreCase) || 
                                              prop.Name.Contains("Arabic", StringComparison.OrdinalIgnoreCase);
                        var isEnglishProperty = prop.Name.EndsWith("En", StringComparison.OrdinalIgnoreCase) || 
                                               prop.Name.Contains("English", StringComparison.OrdinalIgnoreCase);
                        
                        // Skip property if language filter doesn't match
                        if (request.EnglishOnly && isArabicProperty)
                            continue;
                        if (request.ArabicOnly && isEnglishProperty)
                            continue;
                        
                        // Case-insensitive search
                        if (value.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        /// <summary>
        /// Check if entity is active
        /// </summary>
        private bool IsEntityActive(object entity)
        {
            var isActiveProperty = entity.GetType().GetProperty("IsActive");
            if (isActiveProperty?.PropertyType == typeof(bool))
            {
                var value = isActiveProperty.GetValue(entity);
                return value is bool isActive && isActive;
            }
            return true; // Default to active if no IsActive property
        }

        /// <summary>
        /// Get entity category ID
        /// </summary>
        private Guid? GetEntityCategoryId(object entity)
        {
            // Try to find category-related properties
            var entityType = entity.GetType();
            var properties = entityType.GetProperties();
            
            var categoryProperties = properties.Where(p => 
                p.Name.Contains("Category", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Type", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Group", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var prop in categoryProperties)
            {
                var value = prop.GetValue(entity);
                if (value != null)
                {
                    var idProperty = value.GetType().GetProperty("Id");
                    if (idProperty?.GetValue(value) is Guid id)
                    {
                        return id;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Build search result DTO from entity
        /// </summary>
        private SearchResultDto BuildSearchResult(object entity, string entityTypeName, string searchQuery)
        {
            return new SearchResultDto
            {
                Id = GetEntityId(entity),
                EntityType = entityTypeName,
                HierarchyPath = BuildHierarchyPath(entity),
                NavigationRoute = BuildNavigationRoute(entityTypeName, GetEntityId(entity), BuildHierarchyPath(entity)),
                Title = GetEntityTitle(entity),
                TitleEn = GetEntityTitleEn(entity),
                Summary = GetEntitySummary(entity),
                SummaryEn = GetEntitySummaryEn(entity),
                ImageUrl = GetEntityImageUrl(entity),
                Tags = GetEntityTags(entity),
                CreatedTimestamp = GetEntityCreatedTimestamp(entity) ?? DateTime.UtcNow,    
                RelevanceScore = (int)CalculateRelevanceScore(searchQuery, entity),
                Highlights = GenerateHighlights(searchQuery, entity),
                Category = GetEntityCategory(entity)
            };
        }

        /// <summary>
        /// Get entity ID
        /// </summary>
        private Guid GetEntityId(object entity)
        {
            var idProperty = entity.GetType().GetProperty("Id");
            return idProperty?.GetValue(entity) is Guid id ? id : Guid.Empty;
        }

        /// <summary>
        /// Get entity title (Arabic or English)
        /// </summary>
        private string GetEntityTitle(object entity)
        {
            var properties = entity.GetType().GetProperties();
            
            // Try Arabic title first
            var titleAr = properties.FirstOrDefault(p => p.Name.Equals("NameAr", StringComparison.OrdinalIgnoreCase) || 
                                                        p.Name.Equals("TitleAr", StringComparison.OrdinalIgnoreCase));
            if (titleAr?.GetValue(entity) is string arValue && !string.IsNullOrEmpty(arValue))
                return arValue;
            
            // Try English title
            var titleEn = properties.FirstOrDefault(p => p.Name.Equals("NameEn", StringComparison.OrdinalIgnoreCase) || 
                                                        p.Name.Equals("TitleEn", StringComparison.OrdinalIgnoreCase));
            if (titleEn?.GetValue(entity) is string enValue && !string.IsNullOrEmpty(enValue))
                return enValue;
            
            // Try generic name/title
            var title = properties.FirstOrDefault(p => p.Name.Equals("Name", StringComparison.OrdinalIgnoreCase) || 
                                                      p.Name.Equals("Title", StringComparison.OrdinalIgnoreCase));
            if (title?.GetValue(entity) is string value && !string.IsNullOrEmpty(value))
                return value;
            
            return string.Empty;
        }

        /// <summary>
        /// Get entity English title
        /// </summary>
        private string? GetEntityTitleEn(object entity)
        {
            var properties = entity.GetType().GetProperties();
            var titleEn = properties.FirstOrDefault(p => p.Name.Equals("NameEn", StringComparison.OrdinalIgnoreCase) || 
                                                         p.Name.Equals("TitleEn", StringComparison.OrdinalIgnoreCase));
            return titleEn?.GetValue(entity) as string;
        }

        /// <summary>
        /// Get entity summary (Arabic or English)
        /// </summary>
        private string? GetEntitySummary(object entity)
        {
            var properties = entity.GetType().GetProperties();
            
            // Try Arabic summary first
            var summaryAr = properties.FirstOrDefault(p => p.Name.Equals("DescriptionAr", StringComparison.OrdinalIgnoreCase) || 
                                                          p.Name.Equals("SummaryAr", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Name.Equals("ContentAr", StringComparison.OrdinalIgnoreCase));
            if (summaryAr?.GetValue(entity) is string arValue && !string.IsNullOrEmpty(arValue))
                return arValue;
            
            // Try English summary
            var summaryEn = properties.FirstOrDefault(p => p.Name.Equals("DescriptionEn", StringComparison.OrdinalIgnoreCase) || 
                                                          p.Name.Equals("SummaryEn", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Name.Equals("ContentEn", StringComparison.OrdinalIgnoreCase));
            if (summaryEn?.GetValue(entity) is string enValue && !string.IsNullOrEmpty(enValue))
                return enValue;
            
            // Try generic description/summary
            var summary = properties.FirstOrDefault(p => p.Name.Equals("Description", StringComparison.OrdinalIgnoreCase) || 
                                                       p.Name.Equals("Summary", StringComparison.OrdinalIgnoreCase) ||
                                                       p.Name.Equals("Content", StringComparison.OrdinalIgnoreCase));
            return summary?.GetValue(entity) as string;
        }

        /// <summary>
        /// Get entity English summary
        /// </summary>
        private string? GetEntitySummaryEn(object entity)
        {
            var properties = entity.GetType().GetProperties();
            var summaryEn = properties.FirstOrDefault(p => p.Name.Equals("DescriptionEn", StringComparison.OrdinalIgnoreCase) || 
                                                          p.Name.Equals("SummaryEn", StringComparison.OrdinalIgnoreCase) ||
                                                          p.Name.Equals("ContentEn", StringComparison.OrdinalIgnoreCase));
            return summaryEn?.GetValue(entity) as string;
        }

        /// <summary>
        /// Get entity image URL
        /// </summary>
        private string? GetEntityImageUrl(object entity)
        {
            var properties = entity.GetType().GetProperties();
            var imageProperty = properties.FirstOrDefault(p => p.Name.Equals("ImageUrl", StringComparison.OrdinalIgnoreCase) || 
                                                              p.Name.Equals("Image", StringComparison.OrdinalIgnoreCase));
            return imageProperty?.GetValue(entity) as string;
        }

        /// <summary>
        /// Get entity tags
        /// </summary>
        private List<string> GetEntityTags(object entity)
        {
            var properties = entity.GetType().GetProperties();
            var tagProperties = new[] { "Tags", "TagList", "Keywords", "Labels" };
            
            foreach (var tagProp in tagProperties)
            {
                var property = properties.FirstOrDefault(p => p.Name.Equals(tagProp, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    if (property.PropertyType == typeof(List<string>) || property.PropertyType == typeof(IEnumerable<string>))
                    {
                        var value = property.GetValue(entity) as IEnumerable<string>;
                        if (value != null)
                        {
                            return value.Where(t => !string.IsNullOrEmpty(t)).ToList();
                        }
                    }
                }
            }
            
            return new List<string>();
        }

        /// <summary>
        /// Get entity created timestamp
        /// </summary>
        private DateTime? GetEntityCreatedTimestamp(object entity)
        {
            var createdProperty = entity.GetType().GetProperty("CreatedTimestamp");
            return createdProperty?.GetValue(entity) as DateTime?;
        }

        /// <summary>
        /// Calculate relevance score for search result
        /// </summary>
        private double CalculateRelevanceScore(string query, object entity)
        {
            var properties = entity.GetType().GetProperties();
            var score = 0.0;
            var queryLower = query.ToLower();
            
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(entity) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        var valueLower = value.ToLower();
                        
                        // Exact match gets highest score
                        if (valueLower == queryLower)
                            score += 100;
                        // Starts with query gets high score
                        else if (valueLower.StartsWith(queryLower))
                            score += 50;
                        // Contains query gets medium score
                        else if (valueLower.Contains(queryLower))
                            score += 25;
                        
                        // Title properties get bonus points
                        if (prop.Name.Contains("Title") || prop.Name.Contains("Name"))
                            score += 10;
                    }
                }
            }
            
            return score;
        }

        /// <summary>
        /// Generate highlights for search result
        /// </summary>
        private List<string> GenerateHighlights(string query, object entity)
        {
            var highlights = new List<string>();
            var properties = entity.GetType().GetProperties();
            var queryLower = query.ToLower();
            
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(string))
                {
                    var value = prop.GetValue(entity) as string;
                    if (!string.IsNullOrEmpty(value) && value.ToLower().Contains(queryLower))
                    {
                        // Create a snippet around the match
                        var snippet = CreateSnippet(value, query);
                        if (!string.IsNullOrEmpty(snippet))
                        {
                            highlights.Add(snippet);
                        }
                    }
                }
            }
            
            return highlights.Take(3).ToList(); // Limit to 3 highlights
        }

        /// <summary>
        /// Create snippet around search term
        /// </summary>
        private string CreateSnippet(string text, string query)
        {
            var index = text.ToLower().IndexOf(query.ToLower());
            if (index == -1) return string.Empty;
            
            var start = Math.Max(0, index - 50);
            var end = Math.Min(text.Length, index + query.Length + 50);
            var snippet = text.Substring(start, end - start);
            
            if (start > 0) snippet = "..." + snippet;
            if (end < text.Length) snippet = snippet + "...";
            
            return snippet;
        }

        /// <summary>
        /// Get entity category info
        /// </summary>
        private CategoryInfoDto? GetEntityCategory(object entity)
        {
            var categoryEntity = GetEntityCategoryEntity(entity);
            if (categoryEntity == null) return null;

            return new CategoryInfoDto
            {
                Id = GetEntityId(categoryEntity),
                Name = GetEntityTitle(categoryEntity),
                NameEn = GetEntityTitleEn(categoryEntity)
            };
        }

        /// <summary>
        /// Get entity category entity
        /// </summary>
        private object? GetEntityCategoryEntity(object entity)
        {
            var entityType = entity.GetType();
            var properties = entityType.GetProperties();
            
            var categoryProperties = properties.Where(p => 
                p.Name.Contains("Category", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Type", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains("Group", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var prop in categoryProperties)
            {
                var value = prop.GetValue(entity);
                if (value != null) return value;
            }

            return null;
        }

        /// <summary>
        /// Build hierarchy path for navigation
        /// </summary>
        private List<Guid> BuildHierarchyPath(object entity, object? categoryEntity = null)
        {
            var path = new List<Guid>();
            
            // Add category ID if available
            if (categoryEntity != null)
            {
                var categoryId = GetEntityId(categoryEntity);
                if (categoryId != Guid.Empty)
                    path.Add(categoryId);
            }
            
            // Add entity ID
            var entityId = GetEntityId(entity);
            if (entityId != Guid.Empty)
                path.Add(entityId);
            
            return path;
        }

        /// <summary>
        /// Build navigation route for frontend
        /// </summary>
        private string BuildNavigationRoute(string entityType, Guid entityId, List<Guid> hierarchyPath)
        {
            var baseRoute = GetBaseRoute(entityType);
            
            if (hierarchyPath.Count > 1)
            {
                var pathString = string.Join("/", hierarchyPath);
                return $"{baseRoute}/{pathString}";
            }
            
            return $"{baseRoute}/{entityId}";
        }

        /// <summary>
        /// Get base route for entity type
        /// </summary>
        private string GetBaseRoute(string entityType)
        {
            // Convert entity type to route (e.g., "Control" -> "/controls")
            var route = $"/{entityType.ToLower()}s";

            // Handle special cases
            var specialCases = new Dictionary<string, string>
             {
                 //{ "Control", "/standards" },
                 //{ "Safeguard", "/standards" },
                 //{ "Technique", "/standards" },
                 //{ "ImplementationStep", "/standards" },
                 //{ "News", "/news" },
                 //{ "Article", "/articles" },
                 //{ "Video", "/videos" },
                 //{ "Presentation", "/presentations" },
                 //{ "Lecture", "/lectures" }
             };

            return specialCases.TryGetValue(entityType, out var specialRoute) ? specialRoute : route;
        }

        /// <summary>
        /// Get query with safe includes to avoid navigation errors
        /// </summary>
        private IQueryable<object> GetQueryWithSafeIncludes(object dbSet, string entityTypeName)
        {
            var query = (IQueryable<object>)dbSet;
            
            // Try to include common relationship patterns safely
            var commonIncludes = new[] { "Category", "Standard", "Control", "Safeguard", "Technique" };
            
            foreach (var include in commonIncludes)
            {
                try
                {
                    var entityType = dbSet.GetType().GetGenericArguments()[0];
                    var hasProperty = entityType.GetProperties().Any(p => p.Name == include);
                    
                    if (hasProperty)
                    {
                        query = query.Include(include);
                    }
                }
                catch
                {
                    // If Include fails, continue to next
                    continue;
                }
            }
            
            return query;
        }

        /// <summary>
        /// Get DbSet property for entity type
        /// </summary>
        private System.Reflection.PropertyInfo? GetDbSetProperty(string entityTypeName)
        {
            var dbContextType = _context.GetType();
            return dbContextType.GetProperties()
                .FirstOrDefault(p => p.PropertyType.IsGenericType && 
                                   p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                   p.PropertyType.GetGenericArguments()[0].Name == entityTypeName);
        }

        /// <summary>
        /// Get search suggestions for specific entity type
        /// </summary>
        private async Task<List<string>> GetEntitySuggestionsAsync(string entityTypeName, string query, int limit)
        {
            try
            {
                var dbSetProperty = GetDbSetProperty(entityTypeName);
                if (dbSetProperty == null) return new List<string>();

                var dbSet = dbSetProperty.GetValue(_context);
                if (dbSet == null) return new List<string>();

                var objectQuery = (IQueryable<object>)dbSet;
                var entities = await objectQuery.Take(100).ToListAsync(); // Limit for performance
                
                var suggestions = new List<string>();
                foreach (var entity in entities)
                {
                    var title = GetEntityTitle(entity);
                    if (!string.IsNullOrEmpty(title) && title.ToLower().Contains(query.ToLower()))
                    {
                        suggestions.Add(title);
                    }
                }
                
                return suggestions.Take(limit).ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        #endregion
    }
}