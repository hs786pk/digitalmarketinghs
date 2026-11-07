using EarnPlatform.Models;

namespace EarnPlatform.Repositories.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> GetAppSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default);

    Task<ContactSettings> GetContactSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveContactSettingsAsync(ContactSettings settings, CancellationToken cancellationToken = default);

    Task<ChannelSettings> GetChannelSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveChannelSettingsAsync(ChannelSettings settings, CancellationToken cancellationToken = default);

    Task<FbrSettings> GetFbrSettingsAsync(CancellationToken cancellationToken = default);
    Task SaveFbrSettingsAsync(FbrSettings settings, CancellationToken cancellationToken = default);

    Task<SalarySettings> GetSalarySettingsAsync(CancellationToken cancellationToken = default);
    Task SaveSalarySettingsAsync(SalarySettings settings, CancellationToken cancellationToken = default);
}