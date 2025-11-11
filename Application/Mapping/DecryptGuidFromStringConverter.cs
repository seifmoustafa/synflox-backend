using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

public class DecryptGuidFromStringConverter : IValueConverter<string, Guid?>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptGuidFromStringConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid? Convert(string sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            return null;
            
        return _encryption.Decrypt(sourceMember);
    }
}
