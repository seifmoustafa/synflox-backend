using System;
using AutoMapper;
using Application.Services;

namespace Application.Mapping;

/// <summary>
/// AutoMapper converter to decrypt string IDs (encrypted GUIDs) to Guid when mapping from DTOs to Entities
/// </summary>
public class DecryptStringToGuidConverter : IValueConverter<string?, Guid?>
{
    private readonly IIdEncryptionService _encryption;

    public DecryptStringToGuidConverter(IIdEncryptionService encryption)
    {
        _encryption = encryption;
    }

    public Guid? Convert(string? sourceMember, ResolutionContext context)
    {
        if (string.IsNullOrWhiteSpace(sourceMember))
        {
            return null;
        }

        try
        {
            // Decrypt the string (which is an encrypted GUID)
            return _encryption.Decrypt(sourceMember);
        }
        catch
        {
            // If decryption fails, return null (will be handled by validation in service)
            return null;
        }
    }
}



