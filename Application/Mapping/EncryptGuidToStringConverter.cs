using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

public class EncryptGuidToStringConverter : IValueConverter<Guid, string>
{
    private readonly IIdEncryptionService _encryption;

    public EncryptGuidToStringConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public string Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Encrypt(sourceMember).ToString();
}

