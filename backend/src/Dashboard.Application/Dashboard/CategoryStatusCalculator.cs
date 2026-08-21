using Dashboard.Application.Settings;

namespace Dashboard.Application.Dashboard;

/// <summary>
/// The three cutoffs between the four graduated tiers -- configurable (see
/// KnownAppSettings) rather than hardcoded, same rationale as every other
/// rating scale in the app (Net Worth, Social circle size, etc.).
/// </summary>
public sealed record CategoryStatusThresholds(int StrugglingMax, int NeedsAttentionMax, int OnTrackMax);

/// <summary>
/// Turns a 0-100 category score into one of four graduated qualitative reads
/// (plus NoData) everywhere a category score is shown -- the Dashboard's
/// category cards, the overall score, its "Needs Attention" alerts, and, via
/// CategoryDetailService/SocialService, a category's own detail page too.
/// One threshold set read from Settings so those places can never disagree
/// about what a given score means.
/// </summary>
public static class CategoryStatusCalculator
{
    public static CategoryStatus From(int? score, CategoryStatusThresholds thresholds) => score switch
    {
        null => CategoryStatus.NoData,
        _ when score <= thresholds.StrugglingMax => CategoryStatus.Struggling,
        _ when score <= thresholds.NeedsAttentionMax => CategoryStatus.NeedsAttention,
        _ when score <= thresholds.OnTrackMax => CategoryStatus.OnTrack,
        _ => CategoryStatus.Excelling,
    };

    public static async Task<CategoryStatusThresholds> GetThresholdsAsync(
        IAppSettingRepository appSettingRepository, CancellationToken cancellationToken = default) =>
        new(
            StrugglingMax: await AppSettingReader.GetIntAsync(
                appSettingRepository, KnownAppSettings.CategoryStatusStrugglingMax, cancellationToken),
            NeedsAttentionMax: await AppSettingReader.GetIntAsync(
                appSettingRepository, KnownAppSettings.CategoryStatusNeedsAttentionMax, cancellationToken),
            OnTrackMax: await AppSettingReader.GetIntAsync(
                appSettingRepository, KnownAppSettings.CategoryStatusOnTrackMax, cancellationToken));
}
