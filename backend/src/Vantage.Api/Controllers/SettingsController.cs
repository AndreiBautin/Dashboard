using Microsoft.AspNetCore.Mvc;
using Vantage.Api.Contracts;
using Vantage.Application.Settings;

namespace Vantage.Api.Controllers;

/// <summary>
/// A generic editor for every config value in Vantage.Application.Settings.KnownAppSettings
/// -- adding a new configurable setting anywhere in the app requires no
/// change here, only a new registry entry.
/// </summary>
[ApiController]
[Route("api/settings")]
public sealed class SettingsController : ControllerBase
{
    private readonly SettingsService _settingsService;

    public SettingsController(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppSettingSummary>>> GetAll(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetAllAsync(cancellationToken);
        return Ok(settings);
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, UpdateSettingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            await _settingsService.SetAsync(key, request.Value, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
