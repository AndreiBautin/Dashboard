using Vantage.Application.Metrics;
using Vantage.Domain.Settings;

namespace Vantage.Application.Settings;

public sealed class SettingsService
{
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SettingsService(IAppSettingRepository appSettingRepository, IUnitOfWork unitOfWork)
    {
        _appSettingRepository = appSettingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<AppSettingSummary>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _appSettingRepository.GetAllAsync(cancellationToken);
        var storedByKey = stored.ToDictionary(setting => setting.Key, setting => setting.Value);

        return KnownAppSettings.All
            .Select(definition => new AppSettingSummary(
                definition.Key,
                definition.Section,
                definition.Label,
                definition.Description,
                storedByKey.GetValueOrDefault(definition.Key, definition.DefaultValue),
                definition.DefaultValue))
            .ToList();
    }

    /// <summary>
    /// Upserts a setting's value. Validated against its declared
    /// <see cref="AppSettingValueKind"/> up front (see AppSettingReader for
    /// how each kind is parsed back out) rather than persisting a value none
    /// of those readers could ever use.
    /// </summary>
    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var definition = KnownAppSettings.All.FirstOrDefault(d => d.Key == key)
            ?? throw new KeyNotFoundException($"Unknown setting key '{key}'.");

        var isValid = definition.ValueKind switch
        {
            AppSettingValueKind.Integer => int.TryParse(value, out _),
            AppSettingValueKind.Decimal => decimal.TryParse(value, out _),
            AppSettingValueKind.Text => true, // Any string, including empty, is a valid description.
            _ => false,
        };

        if (!isValid)
        {
            var expected = definition.ValueKind switch
            {
                AppSettingValueKind.Integer => "whole number",
                AppSettingValueKind.Decimal => "number",
                _ => "value",
            };
            throw new ArgumentException($"\"{value}\" is not a valid {expected} for \"{definition.Label}\".", nameof(value));
        }

        var existing = await _appSettingRepository.GetAsync(key, cancellationToken);
        if (existing is not null)
        {
            existing.UpdateValue(value);
        }
        else
        {
            await _appSettingRepository.AddAsync(new AppSetting(key, value), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
