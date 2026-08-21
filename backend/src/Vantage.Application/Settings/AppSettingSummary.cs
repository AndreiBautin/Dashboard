namespace Vantage.Application.Settings;

/// <summary>Value is either what's actually stored, or DefaultValue if nothing's been saved yet.</summary>
public sealed record AppSettingSummary(string Key, string Section, string Label, string Description, string Value, string DefaultValue);
