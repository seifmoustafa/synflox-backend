using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper converter for encrypting nullable GUIDs
/// Returns null if source is null, otherwise encrypts the GUID
/// </summary>
public class EncryptNullableGuidConverter : IValueConverter<Guid?, Guid?>
{
    private readonly IIdEncryptionService _encryption;

    public EncryptNullableGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid? Convert(Guid? sourceMember, ResolutionContext context)
    {
        if (!sourceMember.HasValue)
            return null;
            
        return _encryption.Encrypt(sourceMember.Value);
    }
}
