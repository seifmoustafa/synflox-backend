using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Application.Services;
using Domain.Exceptions;
using Infrastructure.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Provides basic file saving functionality based on configuration sections.
/// </summary>
public class FileService : IFileService
{
    private readonly IOptionsSnapshot<FileSettings> _configs;
    private readonly ILocalizationService _localizer;

    public FileService(IOptionsSnapshot<FileSettings> configs, ILocalizationService localizer)
    {
        _configs = configs;
        _localizer = localizer;
    }

    public async Task<string> SaveFileAsync(IFormFile file, string schemeName, string? fileNameWithoutExtension = null)
    {
        if (file == null) throw new ArgumentNullException(nameof(file));
        if (string.IsNullOrWhiteSpace(schemeName)) throw new ArgumentNullException(nameof(schemeName));

        var cfg = _configs.Get(schemeName);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!(cfg.AllowedExtensions.Contains(ext) || cfg.AllowedExtensions.Contains("*")))
        {
            throw new BadRequestException(string.Format(_localizer["FileExtensionNotAllowed"], ext));
        }

        if (file.Length > cfg.MaxFileSizeBytes)
        {
            throw new BadRequestException(string.Format(_localizer["FileTooLarge"], cfg.MaxFileSizeMb));
        }

        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), cfg.StoragePath ?? string.Empty);
        Directory.CreateDirectory(storageRoot);

        var baseName = SanitizeFileName(fileNameWithoutExtension ?? Path.GetFileNameWithoutExtension(file.FileName))
                       ?? Guid.NewGuid().ToString();
        var fileName = baseName + ext;
        var dest = Path.Combine(storageRoot, fileName);
        var counter = 1;
        while (File.Exists(dest))
        {
            fileName = $"{baseName}_{counter++}{ext}";
            dest = Path.Combine(storageRoot, fileName);
        }

        await using var fs = new FileStream(dest, FileMode.Create);
        await file.CopyToAsync(fs);

        return $"{cfg.RequestPath?.TrimEnd('/')}/{fileName}";
    }

    public Task<string> RenameFileAsync(string fileRequestPath, string schemeName, string newFileNameWithoutExtension)
    {
        if (string.IsNullOrWhiteSpace(fileRequestPath)) throw new ArgumentNullException(nameof(fileRequestPath));
        if (string.IsNullOrWhiteSpace(schemeName)) throw new ArgumentNullException(nameof(schemeName));

        var cfg = _configs.Get(schemeName);

        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), cfg.StoragePath ?? string.Empty);
        Directory.CreateDirectory(storageRoot);

        var existingName = Path.GetFileName(fileRequestPath);
        var extension = Path.GetExtension(existingName);
        var newBase = SanitizeFileName(newFileNameWithoutExtension) ?? Guid.NewGuid().ToString();
        var newName = newBase + extension;
        var newFull = Path.Combine(storageRoot, newName);
        var counter = 1;
        while (File.Exists(newFull))
        {
            newName = $"{newBase}_{counter++}{extension}";
            newFull = Path.Combine(storageRoot, newName);
        }

        var existingFull = Path.Combine(storageRoot, existingName);
        if (File.Exists(existingFull))
        {
            File.Move(existingFull, newFull, true);
        }

        return Task.FromResult($"{cfg.RequestPath?.TrimEnd('/')}/{newName}");
    }

    public Task DeleteFileAsync(string fileRequestPath, string schemeName)
    {
        if (string.IsNullOrWhiteSpace(fileRequestPath))
            return Task.CompletedTask;
        if (string.IsNullOrWhiteSpace(schemeName))
            throw new ArgumentNullException(nameof(schemeName));

        var cfg = _configs.Get(schemeName);

        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), cfg.StoragePath ?? string.Empty);
        Directory.CreateDirectory(storageRoot);

        var name = Path.GetFileName(fileRequestPath);
        var full = Path.Combine(storageRoot, name);

        if (File.Exists(full))
        {
            File.Delete(full);
        }

        return Task.CompletedTask;
    }

    public async Task<string> SaveFileFromBytesAsync(byte[] fileData, string fileName, string schemeName)
    {
        if (fileData == null || fileData.Length == 0) throw new ArgumentNullException(nameof(fileData));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentNullException(nameof(fileName));
        if (string.IsNullOrWhiteSpace(schemeName)) throw new ArgumentNullException(nameof(schemeName));

        var cfg = _configs.Get(schemeName);

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!(cfg.AllowedExtensions.Contains(ext) || cfg.AllowedExtensions.Contains("*")))
        {
            throw new BadRequestException(string.Format(_localizer["FileExtensionNotAllowed"], ext));
        }

        if (fileData.Length > cfg.MaxFileSizeBytes)
        {
            throw new BadRequestException(string.Format(_localizer["FileTooLarge"], cfg.MaxFileSizeMb));
        }

        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), cfg.StoragePath ?? string.Empty);
        Directory.CreateDirectory(storageRoot);

        var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(fileName)) ?? Guid.NewGuid().ToString();
        var finalFileName = baseName + ext;
        var dest = Path.Combine(storageRoot, finalFileName);
        var counter = 1;
        while (File.Exists(dest))
        {
            finalFileName = $"{baseName}_{counter++}{ext}";
            dest = Path.Combine(storageRoot, finalFileName);
        }

        await File.WriteAllBytesAsync(dest, fileData);

        return $"{cfg.RequestPath?.TrimEnd('/')}/{finalFileName}";
    }

    private static string? SanitizeFileName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        if (string.IsNullOrWhiteSpace(cleaned)) return null;
        return cleaned.Replace(" ", "_");
    }
}
