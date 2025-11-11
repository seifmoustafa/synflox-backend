using System;
using System.Collections.Generic;

namespace Application.DTOs.Subscriptions;

/// <summary>
/// Request wrapper for decrypting a collection of Module IDs
/// </summary>
public class ModuleIdsRequest
{
    public List<Guid> ModuleIds { get; set; } = new();
}
