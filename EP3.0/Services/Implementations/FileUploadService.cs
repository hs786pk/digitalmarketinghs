using EarnPlatform.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace EarnPlatform.Services.Implementations;

public class FileUploadOptions
{
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
    public string[] AllowedExtensions { get; set; } = { ".jpg", ".jpeg", ".png", ".webp" };
}

/// <summary>
/// Validates and safely persists uploaded images. Never trusts the client-supplied
/// filename or extension alone — inspects the actual leading bytes of the file
/// ("magic numbers") to confirm it is a genuine JPEG, PNG, or WEBP image before
/// saving it under a randomly generated filename.
///
/// IMPORTANT: Blazor Server's IBrowserFile.OpenReadStream() returns a
/// non-seekable stream (it's piped over the SignalR circuit, not a real file
/// handle). This means we cannot read a few header bytes and then Seek(0) back
/// to copy the rest — the stream must be fully buffered into memory first, then
/// both validated and saved from that buffer.
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly FileUploadOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IOptions<FileUploadOptions> options, IWebHostEnvironment environment, ILogger<FileUploadService> logger)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
    }

    public Task<FileUploadResult> SavePaymentScreenshotAsync(Stream fileStream, string originalFileName, long fileSizeBytes, CancellationToken cancellationToken = default) =>
        SaveImageAsync(fileStream, originalFileName, fileSizeBytes, "private/payment-screenshots", cancellationToken);

    public Task<FileUploadResult> SaveFbrImageAsync(Stream fileStream, string originalFileName, long fileSizeBytes, CancellationToken cancellationToken = default) =>
        SaveImageAsync(fileStream, originalFileName, fileSizeBytes, "uploads/fbr", cancellationToken);

    public Stream? OpenPrivateFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;
        try
        {
            var trimmed = relativePath.TrimStart('/', '\\');
            if (!trimmed.StartsWith("private/", StringComparison.OrdinalIgnoreCase)) return null;
            var root = Path.GetFullPath(_environment.ContentRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(root, trimmed.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to open private file {Path}.", relativePath);
            return null;
        }
    }

    public void DeleteFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        try
        {
            var trimmed = relativePath.TrimStart('/', '\\');
            var root = trimmed.StartsWith("private/", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFullPath(_environment.ContentRootPath)
                : Path.GetFullPath(_environment.WebRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(root, trimmed.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Refusing to delete a file outside the application storage roots: {Path}", relativePath);
                return;
            }
            if (File.Exists(fullPath)) File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file at {Path}.", relativePath);
        }
    }

    private async Task<FileUploadResult> SaveImageAsync(Stream fileStream, string originalFileName, long fileSizeBytes, string subfolder, CancellationToken cancellationToken)
    {
        if (fileSizeBytes <= 0 || fileSizeBytes > _options.MaxSizeBytes)
            return new FileUploadResult(false, null, $"File must be smaller than {_options.MaxSizeBytes / 1024 / 1024} MB.");

        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension))
            return new FileUploadResult(false, null, "Only .jpg, .jpeg, .png, and .webp files are allowed.");

        try
        {
            // Buffer the whole upload into memory first — the incoming stream from
            // Blazor Server's InputFile is not seekable, so we can't inspect a
            // header then rewind. Capped at MaxSizeBytes so a client can't force
            // an oversized allocation even if they bypass the size check above.
            using var buffer = new MemoryStream();
            await fileStream.CopyToAsync(buffer, cancellationToken);

            if (buffer.Length == 0)
                return new FileUploadResult(false, null, "The uploaded file is empty.");

            if (buffer.Length > _options.MaxSizeBytes)
                return new FileUploadResult(false, null, $"File must be smaller than {_options.MaxSizeBytes / 1024 / 1024} MB.");

            var bytes = buffer.GetBuffer();
            var detectedType = DetectImageType(bytes, (int)buffer.Length);
            if (detectedType == null)
                return new FileUploadResult(false, null, "The uploaded file is not a valid image.");

            var safeFileName = $"{Guid.NewGuid():N}{detectedType}";
            var storageRoot = subfolder.StartsWith("private/", StringComparison.OrdinalIgnoreCase)
                ? _environment.ContentRootPath
                : _environment.WebRootPath;
            var folderPath = Path.Combine(storageRoot, subfolder.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, safeFileName);
            await File.WriteAllBytesAsync(fullPath, bytes.AsSpan(0, (int)buffer.Length).ToArray(), cancellationToken);

            var relativePath = $"/{subfolder}/{safeFileName}".Replace("\\", "/");
            return new FileUploadResult(true, relativePath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed for {FileName}.", originalFileName);
            return new FileUploadResult(false, null, "Something went wrong saving your file. Please try again.");
        }
    }

    /// <summary>
    /// Checks the file's leading bytes against known image signatures. Returns
    /// the appropriate file extension (".jpg", ".png", ".webp") if recognized,
    /// or null if the bytes don't match any supported image format.
    /// </summary>
    private static string? DetectImageType(byte[] bytes, int length)
    {
        if (length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return ".jpg";

        if (length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return ".png";

        if (length >= 12 &&
            bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
            bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P')
            return ".webp";

        return null;
    }
}