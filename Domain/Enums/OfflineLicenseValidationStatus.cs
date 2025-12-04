namespace Domain.Enums;

/// <summary>
/// Validation status codes for offline license keys.
/// Used to provide detailed feedback on why a license validation failed.
/// </summary>
public enum OfflineLicenseValidationStatus
{
    /// <summary>
    /// License is valid and active
    /// </summary>
    Valid = 0,

    /// <summary>
    /// License key format is invalid (cannot decode)
    /// </summary>
    InvalidFormat = 1,

    /// <summary>
    /// License key signature verification failed (tampered)
    /// </summary>
    TamperedKey = 2,

    /// <summary>
    /// License has expired
    /// </summary>
    Expired = 3,

    /// <summary>
    /// License is in grace period (limited access)
    /// </summary>
    GracePeriod = 4,

    /// <summary>
    /// License is in export-only mode
    /// </summary>
    ExportOnly = 5,

    /// <summary>
    /// License is completely blocked
    /// </summary>
    Blocked = 6,

    /// <summary>
    /// Machine fingerprint doesn't match (wrong machine)
    /// </summary>
    MachineNotAuthorized = 7,

    /// <summary>
    /// System clock tampering detected
    /// </summary>
    ClockTampered = 8,

    /// <summary>
    /// License key has been revoked by administrator
    /// </summary>
    Revoked = 9,

    /// <summary>
    /// License key version is outdated (needs regeneration)
    /// </summary>
    OutdatedVersion = 10,

    /// <summary>
    /// Subscription not found in system
    /// </summary>
    SubscriptionNotFound = 11,

    /// <summary>
    /// Company not found or inactive
    /// </summary>
    CompanyInactive = 12,

    /// <summary>
    /// Internal decryption error
    /// </summary>
    DecryptionError = 13,

    /// <summary>
    /// License not yet activated (future start date)
    /// </summary>
    NotYetActive = 14
}
