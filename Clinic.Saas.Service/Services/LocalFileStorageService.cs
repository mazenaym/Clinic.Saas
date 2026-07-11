using Clinic.Saas.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.IO.Compression;

namespace Clinic.Saas.Service.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly Dictionary<string, string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".png"] = "image/png", [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".rtf"] = "application/rtf"
    };

    private readonly string _root;

    public LocalFileStorageService(IConfiguration configuration)
    {
        var configured = configuration["FileStorage:RootPath"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "storage")
            : configured);
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SavePatientDocumentAsync(Guid tenantId, Guid patientId, string originalFileName,
        string contentType, Stream fileStream, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedDocumentTypes.TryGetValue(extension, out var canonicalType))
            throw new InvalidOperationException("File type is not allowed.");

        await using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        await ValidateContentAsync(buffer, extension, cancellationToken);
        buffer.Position = 0;

        var key = BuildKey(tenantId, "patients", patientId, $"{Guid.NewGuid():N}{extension}");
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (canonicalType.StartsWith("image/", StringComparison.Ordinal))
            await SaveOptimizedImageAsync(buffer, path, extension, 2400, cancellationToken);
        else
            await using (var output = File.Create(path)) await buffer.CopyToAsync(output, cancellationToken);

        return key;
    }

    public async Task<StoredImage> SaveImageAsync(Guid tenantId, string category, Guid ownerId,
        string originalFileName, Stream fileStream, CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await fileStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        using (var probe = SKBitmap.Decode(buffer))
            if (probe is null) throw new InvalidOperationException("Image content is invalid.");
        buffer.Position = 0;

        var key = BuildKey(tenantId, SanitizeSegment(category), ownerId, $"{Guid.NewGuid():N}.webp");
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await SaveOptimizedImageAsync(buffer, path, SKEncodedImageFormat.Webp, 1200, 78, cancellationToken);
        return new StoredImage(key, "image/webp", new FileInfo(path).Length);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        return Task.FromResult<Stream?>(path is not null && File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult(false);
        File.Delete(path);
        return Task.FromResult(true);
    }

    public Task<long?> GetLengthAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        return Task.FromResult<long?>(path is not null && File.Exists(path) ? new FileInfo(path).Length : null);
    }

    private async Task ValidateContentAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        if (extension is ".jpg" or ".jpeg" or ".png")
        {
            using var image = SKBitmap.Decode(stream);
            if (image is null) throw new InvalidOperationException("Image content is invalid.");
            return;
        }

        var header = new byte[Math.Min(8, (int)stream.Length)];
        await stream.ReadExactlyAsync(header, cancellationToken);
        stream.Position = 0;
        if (extension == ".pdf" && !header.AsSpan().StartsWith("%PDF-"u8))
            throw new InvalidOperationException("PDF content is invalid.");
        if (extension == ".rtf" && !header.AsSpan().StartsWith("{\\rtf"u8))
            throw new InvalidOperationException("RTF content is invalid.");
        if (extension == ".docx")
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
                if (archive.GetEntry("[Content_Types].xml") is null || archive.GetEntry("word/document.xml") is null)
                    throw new InvalidOperationException("Word document content is invalid.");
            }
            catch (InvalidDataException) { throw new InvalidOperationException("Word document content is invalid."); }
        }
    }

    private static Task SaveOptimizedImageAsync(Stream input, string path, string extension, int maxDimension, CancellationToken token)
    {
        return SaveOptimizedImageAsync(input, path,
            extension == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg,
            maxDimension, extension == ".png" ? 95 : 80, token);
    }

    private static async Task SaveOptimizedImageAsync(Stream input, string path, SKEncodedImageFormat format,
        int maxDimension, int quality, CancellationToken token)
    {
        using var source = SKBitmap.Decode(input) ?? throw new InvalidOperationException("Image content is invalid.");
        var scale = Math.Min(1d, maxDimension / (double)Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var resized = scale < 1
            ? source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell))
            : source.Copy();
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(format, quality);
        await using var output = File.Create(path);
        data.SaveTo(output);
        await output.FlushAsync(token);
    }

    private static string BuildKey(Guid tenantId, string category, Guid ownerId, string fileName) =>
        string.Join('/', "storage", tenantId.ToString("N"), category, ownerId.ToString("N"), fileName);

    private string Resolve(string key) => TryResolve(key) ?? throw new InvalidOperationException("Invalid storage key.");

    private string? TryResolve(string key)
    {
        var normalized = (key ?? "").Replace('\\', '/').TrimStart('/');
        if (!normalized.StartsWith("storage/", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)) return null;
        var legacy = normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
        var basePath = legacy ? Path.Combine(AppContext.BaseDirectory, "uploads") : _root;
        var relative = normalized[(normalized.IndexOf('/') + 1)..].Replace('/', Path.DirectorySeparatorChar);
        var target = Path.GetFullPath(Path.Combine(basePath, relative));
        var prefix = basePath.EndsWith(Path.DirectorySeparatorChar) ? basePath : basePath + Path.DirectorySeparatorChar;
        return target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? target : null;
    }

    private static string SanitizeSegment(string value) =>
        new string(value.Where(char.IsLetterOrDigit).ToArray()) is { Length: > 0 } clean ? clean.ToLowerInvariant() : "media";
}
