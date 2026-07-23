using Clinic.Saas.Service.Interfaces;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.IO.Compression;

namespace Clinic.Saas.Service.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private const long MaxImagePixels = 40_000_000;
    private const int MaxDocxEntries = 2_000;
    private const long MaxDocxUncompressedBytes = 100 * 1024 * 1024;
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
        CleanupStagedDeletes();
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
        {
            await SaveOptimizedImageAsync(buffer, path, extension, 2400, cancellationToken);
            buffer.Position = 0;
            await SaveOptimizedImageAsync(buffer, ThumbnailPath(path), SKEncodedImageFormat.Webp, 320, 72, cancellationToken);
        }
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
        ValidateImage(buffer);
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
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null);
    }

    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult(false);
        File.Delete(path);
        var thumbnail = ThumbnailPath(path);
        if (File.Exists(thumbnail)) File.Delete(thumbnail);
        return Task.FromResult(true);
    }

    public Task<long?> GetLengthAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        return Task.FromResult<long?>(path is not null && File.Exists(path) ? new FileInfo(path).Length : null);
    }

    public Task<Stream?> OpenThumbnailAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        if (path is null) return Task.FromResult<Stream?>(null);
        var thumbnail = ThumbnailPath(path);
        return Task.FromResult<Stream?>(File.Exists(thumbnail)
            ? new FileStream(thumbnail, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 32 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null);
    }

    public Task<string?> StageDeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = TryResolve(storageKey);
        if (path is null || !File.Exists(path)) return Task.FromResult<string?>(null);
        var suffix = $".deleting.{Guid.NewGuid():N}";
        var staged = path + suffix;
        File.Move(path, staged);
        var thumbnail = ThumbnailPath(path);
        if (File.Exists(thumbnail)) File.Move(thumbnail, ThumbnailPath(staged));
        return Task.FromResult<string?>(storageKey + suffix);
    }

    public Task<bool> RestoreStagedDeleteAsync(string deletionToken, CancellationToken cancellationToken = default)
    {
        var staged = TryResolve(deletionToken);
        if (staged is null || !File.Exists(staged)) return Task.FromResult(false);
        var marker = staged.LastIndexOf(".deleting.", StringComparison.Ordinal);
        if (marker < 0) return Task.FromResult(false);
        var original = staged[..marker];
        File.Move(staged, original, false);
        var stagedThumbnail = ThumbnailPath(staged);
        if (File.Exists(stagedThumbnail)) File.Move(stagedThumbnail, ThumbnailPath(original), false);
        return Task.FromResult(true);
    }

    public Task<bool> CommitStagedDeleteAsync(string deletionToken, CancellationToken cancellationToken = default)
    {
        var staged = TryResolve(deletionToken);
        if (staged is null || !staged.Contains(".deleting.", StringComparison.Ordinal)) return Task.FromResult(false);
        var deleted = false;
        if (File.Exists(staged)) { File.Delete(staged); deleted = true; }
        var thumbnail = ThumbnailPath(staged);
        if (File.Exists(thumbnail)) File.Delete(thumbnail);
        return Task.FromResult(deleted);
    }

    private async Task ValidateContentAsync(Stream stream, string extension, CancellationToken cancellationToken)
    {
        if (extension is ".jpg" or ".jpeg" or ".png")
        {
            ValidateImage(stream);
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
                if (archive.Entries.Count > MaxDocxEntries || archive.Entries.Sum(x => x.Length) > MaxDocxUncompressedBytes)
                    throw new InvalidOperationException("Word document archive is too large.");
                if (archive.Entries.Any(x => x.Length > 0 && x.CompressedLength > 0 && x.Length / Math.Max(1d, x.CompressedLength) > 200))
                    throw new InvalidOperationException("Word document compression ratio is unsafe.");
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
        input.Position = 0;
        using var inputWrapper = new SKManagedStream(input, false);
        using var codec = SKCodec.Create(inputWrapper) ?? throw new InvalidOperationException("Image content is invalid.");
        using var decoded = new SKBitmap(codec.Info);
        if (codec.GetPixels(decoded.Info, decoded.GetPixels()) is not SKCodecResult.Success and not SKCodecResult.IncompleteInput)
            throw new InvalidOperationException("Image content is invalid.");
        using var source = ApplyOrientation(decoded, codec.EncodedOrigin);
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

    private static string ThumbnailPath(string path) => path + ".thumb.webp";

    private static void ValidateImage(Stream stream)
    {
        stream.Position = 0;
        using var streamWrapper = new SKManagedStream(stream, false);
        using var codec = SKCodec.Create(streamWrapper);
        if (codec is null) throw new InvalidOperationException("Image content is invalid.");
        if (codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > MaxImagePixels)
            throw new InvalidOperationException("Image dimensions are too large.");
        if (codec.FrameCount > 1) throw new InvalidOperationException("Animated images are not supported.");
        stream.Position = 0;
    }

    private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
    {
        var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var result = new SKBitmap(swap ? source.Height : source.Width, swap ? source.Width : source.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(result);
        switch (origin)
        {
            case SKEncodedOrigin.TopRight: canvas.Translate(source.Width, 0); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.BottomRight: canvas.Translate(source.Width, source.Height); canvas.RotateDegrees(180); break;
            case SKEncodedOrigin.BottomLeft: canvas.Translate(0, source.Height); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.LeftTop: canvas.RotateDegrees(90); canvas.Scale(1, -1); break;
            case SKEncodedOrigin.RightTop: canvas.Translate(source.Height, 0); canvas.RotateDegrees(90); break;
            case SKEncodedOrigin.RightBottom: canvas.Translate(source.Height, source.Width); canvas.RotateDegrees(90); canvas.Scale(-1, 1); break;
            case SKEncodedOrigin.LeftBottom: canvas.Translate(0, source.Width); canvas.RotateDegrees(270); break;
        }
        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();
        return result;
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

    private void CleanupStagedDeletes()
    {
        var threshold = DateTime.UtcNow.AddHours(-1);
        foreach (var file in Directory.EnumerateFiles(_root, "*.deleting.*", SearchOption.AllDirectories))
        {
            try { if (File.GetLastWriteTimeUtc(file) < threshold) File.Delete(file); }
            catch { /* startup must not fail because a stale deletion is temporarily locked */ }
        }
    }
}
