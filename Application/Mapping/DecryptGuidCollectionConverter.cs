using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

public class DecryptGuidCollectionConverter : IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptGuidCollectionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public IEnumerable<Guid> Convert(IEnumerable<Guid> sourceMember, ResolutionContext context)
        => sourceMember.Select(id => _encryption.Decrypt(id));
}
