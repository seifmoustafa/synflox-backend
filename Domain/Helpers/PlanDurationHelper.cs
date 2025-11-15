using System;
using Domain.Enums;

namespace Domain.Helpers;

/// <summary>
/// Helper class for calculating subscription durations based on plan type
/// </summary>
public static class PlanDurationHelper
{
    /// <summary>
    /// Calculates the expiry date based on start date and plan duration type
    /// </summary>
    public static DateTime CalculateExpiryDate(DateTime startDate, PlanDurationType durationType)
    {
        return durationType switch
        {
            PlanDurationType.Weekly => startDate.AddDays(7),
            PlanDurationType.BiWeekly => startDate.AddDays(14),
            PlanDurationType.Monthly => startDate.AddMonths(1),
            PlanDurationType.Quarterly => startDate.AddMonths(3),
            PlanDurationType.SemiAnnually => startDate.AddMonths(6),
            PlanDurationType.Yearly => startDate.AddYears(1),
            PlanDurationType.Biennial => startDate.AddYears(2),
            PlanDurationType.Triennial => startDate.AddYears(3),
            PlanDurationType.Lifetime => DateTime.MaxValue, // Never expires
            _ => throw new ArgumentException($"Unsupported duration type: {durationType}")
        };
    }

    /// <summary>
    /// Converts PlanDurationType to equivalent months (for backward compatibility)
    /// </summary>
    public static int GetEquivalentMonths(PlanDurationType durationType)
    {
        return durationType switch
        {
            PlanDurationType.Weekly => 0, // Less than a month
            PlanDurationType.BiWeekly => 0, // Less than a month
            PlanDurationType.Monthly => 1,
            PlanDurationType.Quarterly => 3,
            PlanDurationType.SemiAnnually => 6,
            PlanDurationType.Yearly => 12,
            PlanDurationType.Biennial => 24,
            PlanDurationType.Triennial => 36,
            PlanDurationType.Lifetime => 0, // No expiry
            _ => throw new ArgumentException($"Unsupported duration type: {durationType}")
        };
    }

    /// <summary>
    /// Checks if a plan duration type represents a lifetime plan
    /// </summary>
    public static bool IsLifetime(PlanDurationType durationType)
    {
        return durationType == PlanDurationType.Lifetime;
    }

    /// <summary>
    /// Gets a human-readable description of the duration type
    /// </summary>
    public static string GetDurationDescription(PlanDurationType durationType)
    {
        return durationType switch
        {
            PlanDurationType.Weekly => "7 days",
            PlanDurationType.BiWeekly => "14 days",
            PlanDurationType.Monthly => "1 month",
            PlanDurationType.Quarterly => "3 months",
            PlanDurationType.SemiAnnually => "6 months",
            PlanDurationType.Yearly => "1 year",
            PlanDurationType.Biennial => "2 years",
            PlanDurationType.Triennial => "3 years",
            PlanDurationType.Lifetime => "Lifetime (Never Expires)",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Validates if auto-renewal is allowed for this duration type
    /// Lifetime plans cannot auto-renew
    /// </summary>
    public static bool CanAutoRenew(PlanDurationType durationType)
    {
        return durationType != PlanDurationType.Lifetime;
    }

    /// <summary>
    /// Validates if upgrades can be scheduled for this duration type
    /// Lifetime plans cannot schedule upgrades (already permanent)
    /// </summary>
    public static bool CanScheduleUpgrade(PlanDurationType durationType)
    {
        return durationType != PlanDurationType.Lifetime;
    }

    /// <summary>
    /// Validates if subscription can be extended
    /// Lifetime plans cannot be extended (already infinite)
    /// </summary>
    public static bool CanExtend(PlanDurationType durationType)
    {
        return durationType != PlanDurationType.Lifetime;
    }
}
