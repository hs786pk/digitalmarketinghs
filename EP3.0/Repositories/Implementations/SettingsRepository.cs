using EarnPlatform.Data;
using EarnPlatform.Models;
using EarnPlatform.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EarnPlatform.Repositories.Implementations;

public class SettingsRepository : ISettingsRepository
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public SettingsRepository(IDbContextFactory<ApplicationDbContext> factory) => _factory = factory;

    public async Task<AppSettings> GetAppSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var settings = await context.AppSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings != null) return settings;

        settings = new AppSettings
        {
            PaymentAccountName = "Set in Admin Settings",
            PaymentAccountNumber = "0000000000"
        };
        await context.AppSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveAppSettingsAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        settings.UpdatedAt = DateTime.UtcNow;
        context.AppSettings.Update(settings);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContactSettings> GetContactSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ContactSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings != null) return settings;

        settings = new ContactSettings
        {
            WhatsAppAccountName = "Support Team",
            WhatsAppNumber = "0300000000"
        };
        await context.ContactSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveContactSettingsAsync(ContactSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        settings.UpdatedAt = DateTime.UtcNow;
        context.ContactSettings.Update(settings);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChannelSettings> GetChannelSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var settings = await context.ChannelSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings != null) return settings;

        settings = new ChannelSettings
        {
            WhatsAppChannelLink = "https://whatsapp.com/channel/placeholder"
        };
        await context.ChannelSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveChannelSettingsAsync(ChannelSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        settings.UpdatedAt = DateTime.UtcNow;
        context.ChannelSettings.Update(settings);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<FbrSettings> GetFbrSettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var settings = await context.FbrSettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings != null) return settings;

        settings = new FbrSettings { ImagePath = string.Empty };
        await context.FbrSettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveFbrSettingsAsync(FbrSettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        settings.UpdatedAt = DateTime.UtcNow;
        context.FbrSettings.Update(settings);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SalarySettings> GetSalarySettingsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        var settings = await context.SalarySettings.OrderBy(s => s.Id).FirstOrDefaultAsync(cancellationToken);
        if (settings != null) return settings;

        settings = new SalarySettings
        {
            Title = "Salary Program",
            Description = "Details about the platform's salary/bonus program will appear here. This content is managed by the administrator and can be updated at any time."
        };
        await context.SalarySettings.AddAsync(settings, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task SaveSalarySettingsAsync(SalarySettings settings, CancellationToken cancellationToken = default)
    {
        await using var context = await _factory.CreateDbContextAsync(cancellationToken);
        settings.UpdatedAt = DateTime.UtcNow;
        context.SalarySettings.Update(settings);
        await context.SaveChangesAsync(cancellationToken);
    }
}