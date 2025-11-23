using System;
using System.Collections.Generic;

namespace Application.DTOs.ModuleDto;

/// <summary>
/// Request wrapper for decrypting a collection of Module IDs
/// </summary>
public class ModuleIdsRequest
{
    public List<Guid> ModuleIds { get; set; } = new();
}
