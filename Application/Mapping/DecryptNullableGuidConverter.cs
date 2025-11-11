using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

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
            return null;
            
        return _encryption.Decrypt(sourceMember.Value);
    }
}
