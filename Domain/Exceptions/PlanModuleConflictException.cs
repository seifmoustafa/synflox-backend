using System;

namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when plan update has module conflicts that require user confirmation
/// </summary>
public class PlanModuleConflictException : Exception
{
    /// <summary>
    /// The validation result containing conflict details
    /// </summary>
    public object ValidationResult { get; }
    
    public PlanModuleConflictException(object validationResult) 
        : base("Module conflicts detected that require confirmation")
    {
        ValidationResult = validationResult;
    }
}
