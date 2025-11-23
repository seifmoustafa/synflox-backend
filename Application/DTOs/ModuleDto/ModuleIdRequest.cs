using System;

namespace Application.DTOs.ModuleDto;

/// <summary>
/// Request containing encrypted Module ID
/// Used for operations requiring module ID decryption (GetById, Update, Delete)
/// Following SYNFLOX ID Encryption Rule - IDs must be decrypted via AutoMapper
/// </summary>
public class ModuleIdRequest
{
    public Guid ModuleId { get; set; }
}
