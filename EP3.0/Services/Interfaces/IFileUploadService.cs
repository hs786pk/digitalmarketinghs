namespace EarnPlatform.Services.Interfaces;

public record FileUploadResult(bool Succeeded, string? RelativePath, string? ErrorMessage);

public interface IFileUploadService
{
    Task<FileUploadResult> SavePaymentScreenshotAsync(Stream fileStream, string originalFileName, long fileSizeBytes, CancellationToken cancellationToken = default);
    Task<FileUploadResult> SaveFbrImageAsync(Stream fileStream, string originalFileName, long fileSizeBytes, CancellationToken cancellationToken = default);
    Stream? OpenPrivateFile(string? relativePath);
    void DeleteFile(string? relativePath);
}
