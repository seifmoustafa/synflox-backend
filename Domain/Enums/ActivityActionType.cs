namespace Domain.Enums
{
    /// <summary>
    /// Types of actions that can be logged
    /// </summary>
    public enum ActivityActionType
    {
        // CRUD Operations
        Created,
        Updated,
        Deleted,
        
        // Status Changes
        Activated,
        Deactivated,
        Suspended,
        Resumed,
        
        // Subscription Lifecycle
        Renewed,
        Upgraded,
        Cancelled,
        Paused,
        Unpaused,
        Extended,
        Reactivated,
        TrialStopped,
        
        // Authentication
        LoggedIn,
        LoggedOut,
        PasswordChanged,
        TwoFactorEnabled,
        TwoFactorDisabled,
        
        // License
        LicenseGenerated,
        LicenseValidated,
        
        // System
        SystemAction
    }
}
