using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

public class DecryptGuidConverter : IValueConverter<Guid, Guid>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Decrypt(sourceMember);
}
