namespace Infrastructure.Settings;

/// <summary>
/// Settings for Master → Replica database synchronization.
/// Admin API writes to SYNFLOX (Master), then syncs to SYNFLOX_Client (Replica).
/// </summary>
public class DataSyncSettings
{
    /// <summary>
    /// Enable or disable data synchronization
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// How often to sync (in seconds). Default: 5 seconds
    /// </summary>
    public int SyncIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// Maximum records to sync per batch
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
