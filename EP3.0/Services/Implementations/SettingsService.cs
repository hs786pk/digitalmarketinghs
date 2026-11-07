using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using EarnPlatform.Services.Interfaces;

namespace EarnPlatform.Services.Implementations;

public class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IFileUploadService _fileUploadService;

    public SettingsService(ISettingsRepository settingsRepository, IFileUploadService fileUploadService)
    { _settingsRepository = settingsRepository; _fileUploadService = fileUploadService; }

    public Task<AppSettings> GetPaymentSettingsAsync(CancellationToken cancellationToken = default) => _settingsRepository.GetAppSettingsAsync(cancellationToken);

    public async Task UpdatePaymentSettingsAsync(string accountName, string accountNumber, CancellationToken cancellationToken = default)
    {
        accountName = accountName?.Trim() ?? string.Empty; accountNumber = accountNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(accountName) || accountName.Length > 150) throw new InvalidOperationException("Payment account name is required and must be 150 characters or fewer.");
        if (string.IsNullOrWhiteSpace(accountNumber) || accountNumber.Length > 50) throw new InvalidOperationException("Payment account number is required and must be 50 characters or fewer.");
        var settings = await _settingsRepository.GetAppSettingsAsync(cancellationToken);
        settings.PaymentAccountName = accountName; settings.PaymentAccountNumber = accountNumber;
        await _settingsRepository.SaveAppSettingsAsync(settings, cancellationToken);
    }

    public Task<ContactSettings> GetContactSettingsAsync(CancellationToken cancellationToken = default) => _settingsRepository.GetContactSettingsAsync(cancellationToken);

    public async Task UpdateContactSettingsAsync(string whatsAppName, string whatsAppNumber, CancellationToken cancellationToken = default)
    {
        whatsAppName = whatsAppName?.Trim() ?? string.Empty; whatsAppNumber = whatsAppNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(whatsAppName) || whatsAppName.Length > 150) throw new InvalidOperationException("WhatsApp account name is required.");
        if (string.IsNullOrWhiteSpace(whatsAppNumber) || whatsAppNumber.Length > 30) throw new InvalidOperationException("WhatsApp number is required.");
        var settings = await _settingsRepository.GetContactSettingsAsync(cancellationToken);
        settings.WhatsAppAccountName = whatsAppName; settings.WhatsAppNumber = whatsAppNumber;
        await _settingsRepository.SaveContactSettingsAsync(settings, cancellationToken);
    }

    public Task<ChannelSettings> GetChannelSettingsAsync(CancellationToken cancellationToken = default) => _settingsRepository.GetChannelSettingsAsync(cancellationToken);

    public async Task UpdateChannelSettingsAsync(string channelLink, CancellationToken cancellationToken = default)
    {
        channelLink = channelLink?.Trim() ?? string.Empty;
        if (channelLink.Length > 500 || !Uri.TryCreate(channelLink, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("A valid HTTP/HTTPS channel link is required.");
        var settings = await _settingsRepository.GetChannelSettingsAsync(cancellationToken);
        settings.WhatsAppChannelLink = channelLink;
        await _settingsRepository.SaveChannelSettingsAsync(settings, cancellationToken);
    }

    public Task<FbrSettings> GetFbrSettingsAsync(CancellationToken cancellationToken = default) => _settingsRepository.GetFbrSettingsAsync(cancellationToken);

    public async Task UpdateFbrImageAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) throw new InvalidOperationException("FBR image path is required.");
        var settings = await _settingsRepository.GetFbrSettingsAsync(cancellationToken);
        var oldPath = settings.ImagePath;
        settings.ImagePath = imagePath;
        await _settingsRepository.SaveFbrSettingsAsync(settings, cancellationToken);
        _fileUploadService.DeleteFile(oldPath);
    }

    public async Task DeleteFbrImageAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetFbrSettingsAsync(cancellationToken);
        var oldPath = settings.ImagePath;
        settings.ImagePath = string.Empty;
        await _settingsRepository.SaveFbrSettingsAsync(settings, cancellationToken);
        _fileUploadService.DeleteFile(oldPath);
    }

    public Task<SalarySettings> GetSalarySettingsAsync(CancellationToken cancellationToken = default) => _settingsRepository.GetSalarySettingsAsync(cancellationToken);

    public async Task UpdateSalarySettingsAsync(string title, string description, CancellationToken cancellationToken = default)
    {
        title = title?.Trim() ?? string.Empty; description = description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || title.Length > 150) throw new InvalidOperationException("Salary title is required.");
        if (string.IsNullOrWhiteSpace(description) || description.Length > 4000) throw new InvalidOperationException("Salary description is required.");
        var settings = await _settingsRepository.GetSalarySettingsAsync(cancellationToken);
        settings.Title = title; settings.Description = description;
        await _settingsRepository.SaveSalarySettingsAsync(settings, cancellationToken);
    }
}
