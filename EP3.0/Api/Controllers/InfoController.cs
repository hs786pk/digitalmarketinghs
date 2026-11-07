using EarnPlatform.Api.Dtos;
using EarnPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EarnPlatform.Api.Controllers;

[ApiController]
[Route("api/info")]
[Authorize(Policy = "RequireAuthenticatedApi")]
public class InfoController : ControllerBase
{
    private readonly ISettingsService _settings;

    public InfoController(ISettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet("fbr")]
    public async Task<IActionResult> Fbr(CancellationToken ct)
    {
        var s = await _settings.GetFbrSettingsAsync(ct);
        var path = s.ImagePath ?? "";

        string? imageUrl = null;
        if (!string.IsNullOrWhiteSpace(path))
        {
            if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                imageUrl = path;
            }
            else
            {
                // Prefer configured public URL (same as mobile BaseUrl host)
                var publicBase = HttpContext.RequestServices
                    .GetService<IConfiguration>()?["Jwt:Issuer"]
                    ?.TrimEnd('/');

                if (string.IsNullOrWhiteSpace(publicBase))
                    publicBase = $"{Request.Scheme}://{Request.Host}".TrimEnd('/');

                imageUrl = publicBase + "/" + path.TrimStart('/');
            }
        }

        return Ok(new
        {
            imagePath = path,
            imageUrl,
            updatedAt = s.UpdatedAt
        });
    }

    [HttpGet("salary")]
    public async Task<IActionResult> Salary(CancellationToken ct)
    {
        var s = await _settings.GetSalarySettingsAsync(ct);
        return Ok(new
        {
            title = s.Title,
            description = s.Description,
            updatedAt = s.UpdatedAt
        });
    }

    [HttpGet("channel")]
    public async Task<IActionResult> Channel(CancellationToken ct)
    {
        var s = await _settings.GetChannelSettingsAsync(ct);
        return Ok(new
        {
            whatsAppChannelLink = s.WhatsAppChannelLink
        });
    }

    [HttpGet("contact")]
    public async Task<IActionResult> Contact(CancellationToken ct)
    {
        var s = await _settings.GetContactSettingsAsync(ct);
        return Ok(new
        {
            whatsAppAccountName = s.WhatsAppAccountName,
            whatsAppNumber = s.WhatsAppNumber
        });
    }
}