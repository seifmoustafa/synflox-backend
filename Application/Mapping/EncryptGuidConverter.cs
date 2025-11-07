using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

public class EncryptGuidConverter : IValueConverter<Guid, Guid>
{
    private readonly IIdEncryptionService _encryption;

    public EncryptGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Encrypt(sourceMember);
}
