using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper converter to decrypt a list of encrypted Guid IDs when mapping from DTOs
/// </summary>
public class DecryptGuidListConverter : IValueConverter<List<Guid>, List<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptGuidListConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public List<Guid> Convert(List<Guid> sourceMember, ResolutionContext context)
    {
        if (sourceMember == null || sourceMember.Count == 0)
        {
            return new List<Guid>();
        }

        return sourceMember.Select(id => _encryption.Decrypt(id)).ToList();
    }
}



