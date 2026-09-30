using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using EBikeManager.Application.Configuration;
using EBikeManager.Application.Models.Entities;
using EBikeManager.Application.Utils;
using Microsoft.Extensions.Options;

namespace EBikeManager.Application.Services;

public sealed partial class FitArchiveService
{
    private readonly string _root;

    public FitArchiveService(IOptions<EBikeManagerOptions> options)
    {
        _root = Path.GetFullPath(options.Value.FitDirectory);
    }

    public string Root => _root;

    public static string RelativePathFor(Ride ride)
    {
        var local = TimeZones.ToRideLocal(ride.StartTime, ride.TimeZone);
        var safeId = UnsafeFileNameCharacters().Replace(ride.Id, "_");
        return string.Create(CultureInfo.InvariantCulture, $"{local:yyyy}/{local:MM}/{local:yyyy-MM-dd_HHmm}_{safeId}.fit");
    }

    public async Task<string> SaveAsync(string relativePath, byte[] fit, string summaryJson, CancellationToken cancellationToken = default)
    {
        var fitPath = FullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fitPath)!);

        await WriteAtomicallyAsync(Path.ChangeExtension(fitPath, ".json"), Encoding.UTF8.GetBytes(summaryJson), cancellationToken);
        await WriteAtomicallyAsync(fitPath, fit, cancellationToken);
        return Convert.ToHexStringLower(SHA256.HashData(fit));
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var path = FullPath(relativePath);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    public string FullPath(string relativePath)
    {
        var root = _root.EndsWith(Path.DirectorySeparatorChar) ? _root : _root + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        return full.StartsWith(root, StringComparison.Ordinal)
            ? full
            : throw new ArgumentException(relativePath, nameof(relativePath));
    }

    private static async Task WriteAtomicallyAsync(string path, byte[] content, CancellationToken cancellationToken)
    {
        var temporary = path + ".tmp";
        await File.WriteAllBytesAsync(temporary, content, cancellationToken);
        File.Move(temporary, path, overwrite: true);
    }

    [GeneratedRegex("[^A-Za-z0-9_-]")]
    private static partial Regex UnsafeFileNameCharacters();
}
