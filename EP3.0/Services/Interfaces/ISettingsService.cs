using EarnPlatform.Models;

namespace EarnPlatform.Services.Interfaces;

public interface ISettingsService
{
    Task<AppSettings> GetPaymentSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdatePaymentSettingsAsync(string accountName, string accountNumber, CancellationToken cancellationToken = default);

    Task<ContactSettings> GetContactSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateContactSettingsAsync(string whatsAppName, string whatsAppNumber, CancellationToken cancellationToken = default);

    Task<ChannelSettings> GetChannelSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateChannelSettingsAsync(string channelLink, CancellationToken cancellationToken = default);

    Task<FbrSettings> GetFbrSettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateFbrImageAsync(string imagePath, CancellationToken cancellationToken = default);
    Task DeleteFbrImageAsync(CancellationToken cancellationToken = default);

    Task<SalarySettings> GetSalarySettingsAsync(CancellationToken cancellationToken = default);
    Task UpdateSalarySettingsAsync(string title, string description, CancellationToken cancellationToken = default);
}