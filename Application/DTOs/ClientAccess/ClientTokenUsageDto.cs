using System;
using System.Collections.Generic;

namespace Application.DTOs.ClientAccess;

/// <summary>
/// Client token usage statistics and analytics
/// </summary>
public class ClientTokenUsageDto
{
    /// <summary>
    /// Token information
    /// </summary>
    public Guid TokenId { get; set; }
    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Usage statistics
    /// </summary>
    public int TotalUsageCount { get; set; }
    public int UsageCountLast24Hours { get; set; }
    public int UsageCountLast7Days { get; set; }
    public int UsageCountLast30Days { get; set; }

    /// <summary>
    /// Performance metrics
    /// </summary>
    public double AverageResponseTimeMs { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }

    /// <summary>
    /// Endpoint usage breakdown
    /// </summary>
    public Dictionary<string, int> EndpointUsage { get; set; } = new();

    /// <summary>
    /// Recent activity
    /// </summary>
    public List<ClientTokenUsageLogDto> RecentActivity { get; set; } = new();

    /// <summary>
    /// Rate limiting information
    /// </summary>
    public int RateLimitPerHour { get; set; }
    public int RemainingRequestsThisHour { get; set; }
    public DateTime RateLimitResetTime { get; set; }

    /// <summary>
    /// Security information
    /// </summary>
    public List<string> RecentIpAddresses { get; set; } = new();
    public bool HasSuspiciousActivity { get; set; }
    public string? SecurityNotes { get; set; }
}

