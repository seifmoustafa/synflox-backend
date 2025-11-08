using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper converter to decrypt nullable Guid IDs when mapping from DTOs to Entities
/// </summary>
public class DecryptNullableGuidConverter : IValueConverter<Guid?, Guid?>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptNullableGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid? Convert(Guid? sourceMember, ResolutionContext context)
    {
        if (!sourceMember.HasValue)
        {
            return null;
        }

        return _encryption.Decrypt(sourceMember.Value);
    }
}



