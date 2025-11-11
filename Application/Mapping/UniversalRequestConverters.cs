using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// Universal converter for ANY request DTO with a single Guid property to Guid
/// Automatically finds and decrypts any Guid property that ends with "Id"
/// </summary>
public class RequestToGuidConverter<TSource> : ITypeConverter<TSource, Guid>
{
    private readonly IIdEncryptionService _encryption;

    public RequestToGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid Convert(TSource source, Guid destination, ResolutionContext context)
    {
        if (source == null) return Guid.Empty;

        // Find first Guid property (usually named *Id like AdminId, MenuItemId, etc.)
        var guidProperty = typeof(TSource).GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(Guid) && p.Name.EndsWith("Id"));

        if (guidProperty == null)
            throw new InvalidOperationException($"No Guid property ending with 'Id' found on type {typeof(TSource).Name}");

        var encryptedGuid = (Guid)guidProperty.GetValue(source)!;
        return _encryption.Decrypt(encryptedGuid);
    }
}

/// <summary>
/// Universal converter for ANY request DTO with Guid collection to IEnumerable<Guid>
/// Automatically finds and decrypts any IEnumerable<Guid> property
/// </summary>
public class RequestToGuidCollectionConverter<TSource> : ITypeConverter<TSource, IEnumerable<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public RequestToGuidCollectionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public IEnumerable<Guid> Convert(TSource source, IEnumerable<Guid> destination, ResolutionContext context)
    {
        if (source == null) return Enumerable.Empty<Guid>();

        // Find IEnumerable<Guid> property (usually named *Ids like AdminIds)
        var guidsProperty = typeof(TSource).GetProperties()
            .FirstOrDefault(p => typeof(IEnumerable<Guid>).IsAssignableFrom(p.PropertyType) && 
                                 p.PropertyType != typeof(string));

        if (guidsProperty == null)
            throw new InvalidOperationException($"No IEnumerable<Guid> property found on type {typeof(TSource).Name}");

        var encryptedGuids = (IEnumerable<Guid>)guidsProperty.GetValue(source)!;
        return encryptedGuids.Select(id => _encryption.Decrypt(id));
    }
}

/// <summary>
/// Converter for decrypting a List<Guid> of encrypted IDs directly
/// Used when you need to decrypt a specific property (e.g., ModuleIds, ProjectIds)
/// </summary>
public class GuidCollectionConverter : IValueConverter<List<Guid>, List<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public GuidCollectionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public List<Guid> Convert(List<Guid> sourceMember, ResolutionContext context)
    {
        if (sourceMember == null || !sourceMember.Any())
            return new List<Guid>();

        return sourceMember.Select(id => _encryption.Decrypt(id)).ToList();
    }
}

/// <summary>
/// Converter for decrypting a nullable List<Guid> of encrypted IDs directly
/// Used when you need to decrypt optional collections (e.g., in Update DTOs)
/// </summary>
public class NullableGuidCollectionConverter : IValueConverter<List<Guid>?, List<Guid>?>
{
    private readonly IIdEncryptionService _encryption;

    public NullableGuidCollectionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public List<Guid>? Convert(List<Guid>? sourceMember, ResolutionContext context)
    {
        if (sourceMember == null)
            return null;

        if (!sourceMember.Any())
            return new List<Guid>();

        return sourceMember.Select(id => _encryption.Decrypt(id)).ToList();
    }
}
