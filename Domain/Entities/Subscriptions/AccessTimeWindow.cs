using System;
using System.ComponentModel.DataAnnotations;
using Domain.Entities.Common;

namespace Domain.Entities.Subscriptions;

/// <summary>
/// Defines a time window during which device access is allowed.
/// Used with TimeBasedUnlimited and TimeBasedLimited concurrent access modes.
/// Multiple windows can be configured per plan for complex schedules.
/// </summary>
public class AccessTimeWindow : AuditEntity<Guid>
{
    /// <summary>
    /// The plan this time window belongs to.
    /// </summary>
    public Guid PlanId { get; set; }

    /// <summary>
    /// Specific day of week this window applies to.
    /// null = applies to all days.
    /// </summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>
    /// Start time of the access window (local time based on TimezoneId).
    /// Example: 09:00 for 9 AM.
    /// </summary>
    public TimeOnly StartTime { get; set; }

    /// <summary>
    /// End time of the access window (local time based on TimezoneId).
    /// Example: 18:00 for 6 PM.
    /// Note: If EndTime < StartTime, window spans midnight.
    /// </summary>
    public TimeOnly EndTime { get; set; }

    /// <summary>
    /// IANA timezone identifier (e.g., "America/New_York", "Europe/London", "Africa/Cairo").
    /// null = use the company's configured timezone.
    /// </summary>
    [StringLength(100)]
    public string? TimezoneId { get; set; }

    /// <summary>
    /// Friendly name for this time window (e.g., "Business Hours", "Night Shift").
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// Whether this time window is currently active.
    /// Allows disabling windows without deleting them.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for UI presentation.
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public virtual SubscriptionPlan Plan { get; set; } = null!;

    // Computed properties

    /// <summary>
    /// Returns true if the window applies to all days.
    /// </summary>
    public bool AppliesToAllDays => !DayOfWeek.HasValue;

    /// <summary>
    /// Returns true if the window spans midnight (end time is before start time).
    /// Example: 22:00 - 06:00 (10 PM to 6 AM).
    /// </summary>
    public bool SpansMidnight => EndTime < StartTime;

    /// <summary>
    /// Check if the given UTC time falls within this window.
    /// </summary>
    /// <param name="utcNow">UTC time to check</param>
    /// <param name="companyTimezoneId">Company's default timezone (used if TimezoneId is null)</param>
    /// <returns>True if within window</returns>
    public bool IsWithinWindow(DateTime utcNow, string? companyTimezoneId = null)
    {
        if (!IsActive) return false;

        // Determine timezone
        var tzId = TimezoneId ?? companyTimezoneId ?? "UTC";
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        }
        catch
        {
            // Fallback to UTC if timezone not found
            tz = TimeZoneInfo.Utc;
        }

        // Convert UTC to local time
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var localTimeOnly = TimeOnly.FromDateTime(localTime);
        var localDayOfWeek = localTime.DayOfWeek;

        // Check day of week
        if (DayOfWeek.HasValue && DayOfWeek.Value != localDayOfWeek)
        {
            // For midnight-spanning windows, also check previous day
            if (SpansMidnight)
            {
                var previousDay = localDayOfWeek == System.DayOfWeek.Sunday 
                    ? System.DayOfWeek.Saturday 
                    : (DayOfWeek)(((int)localDayOfWeek - 1 + 7) % 7);
                
                if (DayOfWeek.Value != previousDay)
                    return false;
            }
            else
            {
                return false;
            }
        }

        // Check time
        if (SpansMidnight)
        {
            // Window spans midnight: valid if time >= start OR time <= end
            return localTimeOnly >= StartTime || localTimeOnly <= EndTime;
        }
        else
        {
            // Normal window: valid if time >= start AND time <= end
            return localTimeOnly >= StartTime && localTimeOnly <= EndTime;
        }
    }
}
