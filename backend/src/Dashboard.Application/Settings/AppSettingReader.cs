namespace Dashboard.Application.Settings;

/// <summary>
/// Shared "read this known setting, falling back to its default if unset or
/// unparsable" logic -- used by any service that needs the live numeric
/// value (not the raw string) of a setting. Centralized so SocialService,
/// CategoryDetailService, and MetricEntryService don't each reimplement the
/// same fallback rule.
/// </summary>
public static class AppSettingReader
{
    public static async Task<int> GetIntAsync(
        IAppSettingRepository repository, AppSettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var raw = await GetRawAsync(repository, definition, cancellationToken);
        return int.TryParse(raw, out var value) ? value : int.Parse(definition.DefaultValue);
    }

    public static async Task<decimal> GetDecimalAsync(
        IAppSettingRepository repository, AppSettingDefinition definition, CancellationToken cancellationToken = default)
    {
        var raw = await GetRawAsync(repository, definition, cancellationToken);
        return decimal.TryParse(raw, out var value) ? value : decimal.Parse(definition.DefaultValue);
    }

    /// <summary>Freeform text settings (e.g. rating descriptions) need no parsing -- just the stored-or-default string.</summary>
    public static Task<string> GetTextAsync(
        IAppSettingRepository repository, AppSettingDefinition definition, CancellationToken cancellationToken = default) =>
        GetRawAsync(repository, definition, cancellationToken);

    private static async Task<string> GetRawAsync(
        IAppSettingRepository repository, AppSettingDefinition definition, CancellationToken cancellationToken)
    {
        var setting = await repository.GetAsync(definition.Key, cancellationToken);
        return setting?.Value ?? definition.DefaultValue;
    }
}
