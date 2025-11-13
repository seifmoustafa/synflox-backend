using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// Universal decryption converter that handles ALL decryption scenarios in SYNFLOX
/// Replaces: DecryptGuidConverter, DecryptNullableGuidConverter, DecryptGuidFromStringConverter,
///           DecryptGuidCollectionConverter, RequestToGuidConverter, RequestToGuidCollectionConverter,
///           GuidCollectionConverter, NullableGuidCollectionConverter
/// Supports: Guid, Guid?, string input, collections, request DTOs with reflection
/// </summary>
public class UniversalDecryptionConverter : 
    IValueConverter<Guid, Guid>,
    IValueConverter<Guid?, Guid?>,
    IValueConverter<string, Guid?>,
    IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>,
    ITypeConverter<object, Guid>,
    ITypeConverter<object, IEnumerable<Guid>>
{
    private readonly IIdEncryptionService _encryption;

    public UniversalDecryptionConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    /// <summary>
    /// Decrypt Guid → Guid (most common case: DTO.Id → Entity.Id)
    /// </summary>
    Guid IValueConverter<Guid, Guid>.Convert(Guid sourceMember, ResolutionContext context)
        => _encryption.Decrypt(sourceMember);

    /// <summary>
    /// Decrypt Guid? → Guid? (nullable IDs: DTO.ParentId → Entity.ParentId)
    /// </summary>
    Guid? IValueConverter<Guid?, Guid?>.Convert(Guid? sourceMember, ResolutionContext context)
        => sourceMember.HasValue ? _encryption.Decrypt(sourceMember.Value) : null;

    /// <summary>
    /// Decrypt string → Guid? (string-based encrypted IDs from API)
    /// </summary>
    Guid? IValueConverter<string, Guid?>.Convert(string sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
            return null;
            
        return _encryption.Decrypt(sourceMember);
    }

    /// <summary>
    /// Decrypt IEnumerable&lt;Guid&gt; → IEnumerable&lt;Guid&gt; (collections)
    /// </summary>
    IEnumerable<Guid> IValueConverter<IEnumerable<Guid>, IEnumerable<Guid>>.Convert(IEnumerable<Guid> sourceMember, ResolutionContext context)
        => sourceMember?.Select(id => _encryption.Decrypt(id)) ?? Enumerable.Empty<Guid>();

    /// <summary>
    /// Universal Request DTO → Guid converter (replaces RequestToGuidConverter)
    /// Automatically finds and decrypts any Guid property ending with "Id"
    /// Works with: GetAdminByIdRequest, GetCompanyByIdRequest, etc.
    /// </summary>
    Guid ITypeConverter<object, Guid>.Convert(object source, Guid destination, ResolutionContext context)
    {
        if (source == null) return Guid.Empty;

        // Find first Guid property ending with "Id" (AdminId, CompanyId, MenuItemId, etc.)
        var guidProperty = source.GetType().GetProperties()
            .FirstOrDefault(p => p.PropertyType == typeof(Guid) && p.Name.EndsWith("Id"));

        if (guidProperty == null)
            throw new InvalidOperationException($"No Guid property ending with 'Id' found on type {source.GetType().Name}");

        var encryptedGuid = (Guid)guidProperty.GetValue(source)!;
        return _encryption.Decrypt(encryptedGuid);
    }

    /// <summary>
    /// Universal Request DTO → IEnumerable&lt;Guid&gt; converter (replaces RequestToGuidCollectionConverter)
    /// Automatically finds and decrypts any IEnumerable&lt;Guid&gt; property
    /// Works with: AdminIdsRequest, ModuleIdsRequest, etc.
    /// </summary>
    IEnumerable<Guid> ITypeConverter<object, IEnumerable<Guid>>.Convert(object source, IEnumerable<Guid> destination, ResolutionContext context)
    {
        if (source == null) return Enumerable.Empty<Guid>();

        // Find IEnumerable<Guid> property (AdminIds, ModuleIds, etc.)
        var guidsProperty = source.GetType().GetProperties()
            .FirstOrDefault(p => typeof(IEnumerable<Guid>).IsAssignableFrom(p.PropertyType) && 
                                 p.PropertyType != typeof(string));

        if (guidsProperty == null)
            throw new InvalidOperationException($"No IEnumerable<Guid> property found on type {source.GetType().Name}");

        var encryptedGuids = (IEnumerable<Guid>)guidsProperty.GetValue(source)!;
        return encryptedGuids.Select(id => _encryption.Decrypt(id));
    }
}
