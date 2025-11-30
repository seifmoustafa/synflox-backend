namespace Domain.Enums;

/// <summary>
/// Defines how an entitlement was granted to a subscription
/// </summary>
public enum EntitlementGrantType
{
    /// <summary>
    /// Default/uninitialized value
    /// </summary>
    None = 0,

    /// <summary>
    /// Access to entire project and ALL its modules (current + future)
    /// When a new module is added to the project, access is automatically granted
    /// </summary>
    FullProject = 1,

    /// <summary>
    /// Access to specific modules within a project
    /// Only the explicitly listed modules are accessible
    /// </summary>
    SpecificModules = 2,

    /// <summary>
    /// Access to a standalone module (not tied to any project)
    /// Used for cross-project features or standalone tools
    /// </summary>
    StandaloneModule = 3,

    /// <summary>
    /// Access to specific features only within a module
    /// Most granular level of access control
    /// </summary>
    FeatureOnly = 4
}
