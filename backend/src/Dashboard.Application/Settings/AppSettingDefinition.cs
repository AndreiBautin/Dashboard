namespace Dashboard.Application.Settings;

/// <summary>What kind of value a setting holds, so SettingsService can validate an
/// edit and readers know how to parse it -- whole numbers (friend counts,
/// month windows) versus numbers that may have a fractional part (e.g. an
/// arm-measurement rating threshold in inches).</summary>
public enum AppSettingValueKind
{
    Integer,
    Decimal,
    Text,
}

/// <summary>
/// Describes one known, editable config value -- the label/description/default
/// a generic Settings page renders, independent of whether a row for it
/// exists in the app_settings table yet (it may not, if it's never been
/// overridden from its default). <see cref="Section"/> is purely a display
/// grouping (e.g. "Social", "Finance") so the Settings page can organize a
/// growing list of settings without the frontend needing to know anything
/// about what each one means.
/// </summary>
public sealed record AppSettingDefinition(
    string Key,
    string Section,
    string Label,
    string Description,
    string DefaultValue,
    AppSettingValueKind ValueKind = AppSettingValueKind.Integer);
