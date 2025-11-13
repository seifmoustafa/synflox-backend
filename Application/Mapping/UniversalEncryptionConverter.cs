using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// Universal encryption converter that handles ALL encryption scenarios in SYNFLOX
/// Replaces: EncryptGuidConverter, EncryptNullableGuidConverter, EncryptGuidToStringConverter
/// Supports: Guid, Guid?, string output, collections, and nullable collections
/// </summary>
public class UniversalEncryptionConverter : 
    IValueConverter<Guid, Guid>,
    IValueConverter<Guid?, Guid?>,
    IValueConverter<Guid, string>,
    IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public UniversalEncryptionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    /// <summary>
    /// Encrypt Guid → Guid (most common case: Entity.Id → DTO.Id)
    /// </summary>
    Guid IValueConverter<Guid, Guid>.Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Encrypt(sourceMember);

    /// <summary>
    /// Encrypt Guid? → Guid? (nullable IDs: Entity.ParentId → DTO.ParentId)
    /// </summary>
    Guid? IValueConverter<Guid?, Guid?>.Convert(Guid? sourceMember, ResolutionContext context)
        => sourceMember.HasValue ? _encryption.Encrypt(sourceMember.Value) : null;

    /// <summary>
    /// Encrypt Guid → string (for string-based API responses)
    /// </summary>
    string IValueConverter<Guid, string>.Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Encrypt(sourceMember).ToString();

    /// <summary>
    /// Encrypt IEnumerable&lt;Guid&gt; → IEnumerable&lt;Guid&gt; (collections)
    /// </summary>
    IEnumerable<Guid> IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>.Convert(IEnumerable<Guid> sourceMember, ResolutionContext context)
        => sourceMember?.Select(id => _encryption.Encrypt(id)) ?? Enumerable.Empty<Guid>();
}
