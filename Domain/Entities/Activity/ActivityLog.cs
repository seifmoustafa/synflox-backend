using Domain.Entities.Common;

namespace Domain.Entities.Activity
{
    /// <summary>
    /// Tracks all system activities for audit and dashboard display
    /// </summary>
    public class ActivityLog : BaseEntity<Guid>
    {
        /// <summary>
        /// Type of action performed (Created, Updated, Deleted, Activated, Suspended, etc.)
        /// </summary>
        public string ActionType { get; set; } = string.Empty;

        /// <summary>
        /// Type of entity affected (Company, Subscription, Admin, Plan, etc.)
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// ID of the affected entity
        /// </summary>
        public Guid EntityId { get; set; }

        /// <summary>
        /// Display name of the affected entity (for quick display without joins)
        /// </summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the action
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Admin who performed the action (null for system actions)
        /// </summary>
        public Guid? PerformedBy { get; set; }

        /// <summary>
        /// Name of the admin who performed the action
        /// </summary>
        public string? PerformedByName { get; set; }

        /// <summary>
        /// When the action was performed
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional metadata in JSON format (old values, new values, etc.)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// IP address of the request (if available)
        /// </summary>
        public string? IpAddress { get; set; }
    }
}
