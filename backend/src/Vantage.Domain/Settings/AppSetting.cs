namespace Vantage.Domain.Settings;

/// <summary>
/// A small key/value config row — e.g. ActiveCircleThresholdMonths — so
/// values like the Social active-circle window are editable without a code
/// change, per the Phase 0 design (docs/phase-0-design-proposal.md §6).
/// </summary>
public sealed class AppSetting
{
    public string Key { get; private set; } = null!;
    public string Value { get; private set; } = null!;

    // For EF Core materialization only.
    private AppSetting()
    {
    }

    public AppSetting(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key is required.", nameof(key));
        }

        Key = key;
        Value = value;
    }

    public void UpdateValue(string value)
    {
        Value = value;
    }
}
