using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper resolver to decrypt nullable Guid IDs when mapping from DTOs to Entities
/// </summary>
public class DecryptNullableGuidResolver : IMemberValueResolver<object, object, Guid?, Guid?>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptNullableGuidResolver(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid? Resolve(object source, object destination, Guid? sourceMember, Guid? destMember, ResolutionContext context)
    {
        if (!sourceMember.HasValue)
        {
            return null;
        }

        return _encryption.Decrypt(sourceMember.Value);
    }
}

