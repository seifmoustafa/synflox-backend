using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper converter to decrypt Guid IDs when mapping from DTOs to Entities
/// </summary>
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





