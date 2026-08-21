using Vantage.Application.Settings;
using Vantage.Domain.Metrics;

namespace Vantage.Application.Metrics;

/// <summary>
/// Ties one metric (matched by name -- metrics are looked up by name
/// elsewhere in this codebase too, e.g. DevelopmentDataSeeder, since this is
/// a single-user local app where category/metric names are effectively
/// stable identifiers) to the three AppSettings that hold its rating
/// thresholds, the four display labels for its tiers, and the four
/// (configurable) descriptions shown alongside those labels. Thresholds and
/// descriptions are both editable from the Settings page; labels are fixed,
/// since they're just short display text, not something worth a settings row.
/// </summary>
public sealed record MetricRatingDefinition(
    string MetricName,
    AppSettingDefinition Tier1MaxSetting,
    AppSettingDefinition Tier2MaxSetting,
    AppSettingDefinition Tier3MaxSetting,
    string Tier1Label,
    string Tier2Label,
    string Tier3Label,
    string Tier4Label,
    AppSettingDefinition Tier1DescriptionSetting,
    AppSettingDefinition Tier2DescriptionSetting,
    AppSettingDefinition Tier3DescriptionSetting,
    AppSettingDefinition Tier4DescriptionSetting,
    // True for most rated metrics (Net Worth, Arm Measurement, ...) where a
    // bigger number is the better outcome. False for metrics like Waist
    // Measurement where a smaller number is better -- see
    // MetricRatingThresholds.HigherIsBetter for how this flows through to
    // tier/score calculation.
    bool HigherIsBetter = true)
{
    public string LabelFor(MetricRatingTier tier) => tier switch
    {
        MetricRatingTier.Tier1 => Tier1Label,
        MetricRatingTier.Tier2 => Tier2Label,
        MetricRatingTier.Tier3 => Tier3Label,
        MetricRatingTier.Tier4 => Tier4Label,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    private AppSettingDefinition DescriptionSettingFor(MetricRatingTier tier) => tier switch
    {
        MetricRatingTier.Tier1 => Tier1DescriptionSetting,
        MetricRatingTier.Tier2 => Tier2DescriptionSetting,
        MetricRatingTier.Tier3 => Tier3DescriptionSetting,
        MetricRatingTier.Tier4 => Tier4DescriptionSetting,
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, null),
    };

    public Task<string> GetDescriptionAsync(
        MetricRatingTier tier, IAppSettingRepository appSettingRepository, CancellationToken cancellationToken = default) =>
        AppSettingReader.GetTextAsync(appSettingRepository, DescriptionSettingFor(tier), cancellationToken);

    public async Task<MetricRatingThresholds> GetThresholdsAsync(
        IAppSettingRepository appSettingRepository, CancellationToken cancellationToken = default) =>
        new(
            Tier1Max: await AppSettingReader.GetDecimalAsync(appSettingRepository, Tier1MaxSetting, cancellationToken),
            Tier2Max: await AppSettingReader.GetDecimalAsync(appSettingRepository, Tier2MaxSetting, cancellationToken),
            Tier3Max: await AppSettingReader.GetDecimalAsync(appSettingRepository, Tier3MaxSetting, cancellationToken),
            HigherIsBetter: HigherIsBetter);

    /// <summary>
    /// The full scale, ascending by raw value, each row paired with whichever
    /// label actually applies there. Reuses <see cref="MetricRatingCalculator.Rate"/>
    /// itself (evaluated once per cutoff, plus once just past the last one)
    /// rather than re-deriving the worst/best-end logic here, so a
    /// HigherIsBetter: false metric like Waist Measurement automatically gets
    /// its bands in the correct (inverted) label order without this method
    /// needing to know that direction exists.
    /// </summary>
    public IReadOnlyList<RatingBand> DescribeBands(MetricRatingThresholds thresholds) =>
    [
        new(thresholds.Tier1Max, LabelFor(MetricRatingCalculator.Rate(thresholds.Tier1Max, thresholds))),
        new(thresholds.Tier2Max, LabelFor(MetricRatingCalculator.Rate(thresholds.Tier2Max, thresholds))),
        new(thresholds.Tier3Max, LabelFor(MetricRatingCalculator.Rate(thresholds.Tier3Max, thresholds))),
        new(null, LabelFor(MetricRatingCalculator.Rate(thresholds.Tier3Max + 1, thresholds))),
    ];
}

/// <summary>
/// Reasonable starting-point thresholds/labels/descriptions for the four
/// metrics that currently want a rating (Net Worth, Credit Score, Strength
/// Total, Arm Measurement) -- all tunable per-user via the Settings page,
/// since "reasonable" here is necessarily a rough guess rather than anything
/// personal to any one person's numbers.
/// </summary>
public static class KnownMetricRatings
{
    public static readonly MetricRatingDefinition NetWorth = new(
        MetricName: "Net Worth",
        Tier1MaxSetting: KnownAppSettings.NetWorthTier1Max,
        Tier2MaxSetting: KnownAppSettings.NetWorthTier2Max,
        Tier3MaxSetting: KnownAppSettings.NetWorthTier3Max,
        Tier1Label: "Building",
        Tier2Label: "Growing",
        Tier3Label: "Strong",
        Tier4Label: "Thriving",
        Tier1DescriptionSetting: KnownAppSettings.NetWorthTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.NetWorthTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.NetWorthTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.NetWorthTier4Description);

    public static readonly MetricRatingDefinition CreditScore = new(
        MetricName: "Credit Score",
        Tier1MaxSetting: KnownAppSettings.CreditScoreTier1Max,
        Tier2MaxSetting: KnownAppSettings.CreditScoreTier2Max,
        Tier3MaxSetting: KnownAppSettings.CreditScoreTier3Max,
        Tier1Label: "Poor",
        Tier2Label: "Fair",
        Tier3Label: "Good",
        Tier4Label: "Excellent",
        Tier1DescriptionSetting: KnownAppSettings.CreditScoreTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.CreditScoreTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.CreditScoreTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.CreditScoreTier4Description);

    /// <summary>
    /// Tier4's label is "Well Funded" rather than "Fully Funded" on purpose:
    /// "fully" is an absolute claim that a partial (sub-100) score would
    /// directly contradict, whereas every other metric's top label (e.g. Net
    /// Worth's "Thriving", Strength Total's "Elite") is a relative
    /// superlative that a continuous score can sit anywhere within without
    /// reading as self-contradictory. Same open-ended scoring as every other
    /// rated metric otherwise -- see MetricScoring's month-1 rated-value
    /// fallback for why a fresh metric can show a sub-100 score here at all.
    /// </summary>
    public static readonly MetricRatingDefinition EmergencyFund = new(
        MetricName: "Emergency Fund",
        Tier1MaxSetting: KnownAppSettings.EmergencyFundTier1Max,
        Tier2MaxSetting: KnownAppSettings.EmergencyFundTier2Max,
        Tier3MaxSetting: KnownAppSettings.EmergencyFundTier3Max,
        Tier1Label: "Starting",
        Tier2Label: "Building",
        Tier3Label: "Almost There",
        Tier4Label: "Well Funded",
        Tier1DescriptionSetting: KnownAppSettings.EmergencyFundTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.EmergencyFundTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.EmergencyFundTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.EmergencyFundTier4Description);

    public static readonly MetricRatingDefinition RetirementFund = new(
        MetricName: "Retirement Fund",
        Tier1MaxSetting: KnownAppSettings.RetirementFundTier1Max,
        Tier2MaxSetting: KnownAppSettings.RetirementFundTier2Max,
        Tier3MaxSetting: KnownAppSettings.RetirementFundTier3Max,
        Tier1Label: "Starting",
        Tier2Label: "Building",
        Tier3Label: "Strong",
        Tier4Label: "Thriving",
        Tier1DescriptionSetting: KnownAppSettings.RetirementFundTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.RetirementFundTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.RetirementFundTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.RetirementFundTier4Description);

    public static readonly MetricRatingDefinition StrengthTotal = new(
        MetricName: "Strength Total",
        Tier1MaxSetting: KnownAppSettings.StrengthTotalTier1Max,
        Tier2MaxSetting: KnownAppSettings.StrengthTotalTier2Max,
        Tier3MaxSetting: KnownAppSettings.StrengthTotalTier3Max,
        Tier1Label: "Beginner",
        Tier2Label: "Intermediate",
        Tier3Label: "Advanced",
        Tier4Label: "Elite",
        Tier1DescriptionSetting: KnownAppSettings.StrengthTotalTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.StrengthTotalTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.StrengthTotalTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.StrengthTotalTier4Description);

    public static readonly MetricRatingDefinition Squat = new(
        MetricName: "Squat",
        Tier1MaxSetting: KnownAppSettings.SquatTier1Max,
        Tier2MaxSetting: KnownAppSettings.SquatTier2Max,
        Tier3MaxSetting: KnownAppSettings.SquatTier3Max,
        Tier1Label: "Beginner",
        Tier2Label: "Intermediate",
        Tier3Label: "Advanced",
        Tier4Label: "Elite",
        Tier1DescriptionSetting: KnownAppSettings.SquatTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.SquatTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.SquatTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.SquatTier4Description);

    public static readonly MetricRatingDefinition BenchPress = new(
        MetricName: "Bench Press",
        Tier1MaxSetting: KnownAppSettings.BenchPressTier1Max,
        Tier2MaxSetting: KnownAppSettings.BenchPressTier2Max,
        Tier3MaxSetting: KnownAppSettings.BenchPressTier3Max,
        Tier1Label: "Beginner",
        Tier2Label: "Intermediate",
        Tier3Label: "Advanced",
        Tier4Label: "Elite",
        Tier1DescriptionSetting: KnownAppSettings.BenchPressTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.BenchPressTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.BenchPressTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.BenchPressTier4Description);

    public static readonly MetricRatingDefinition Deadlift = new(
        MetricName: "Deadlift",
        Tier1MaxSetting: KnownAppSettings.DeadliftTier1Max,
        Tier2MaxSetting: KnownAppSettings.DeadliftTier2Max,
        Tier3MaxSetting: KnownAppSettings.DeadliftTier3Max,
        Tier1Label: "Beginner",
        Tier2Label: "Intermediate",
        Tier3Label: "Advanced",
        Tier4Label: "Elite",
        Tier1DescriptionSetting: KnownAppSettings.DeadliftTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.DeadliftTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.DeadliftTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.DeadliftTier4Description);

    public static readonly MetricRatingDefinition OverheadPress = new(
        MetricName: "Overhead Press",
        Tier1MaxSetting: KnownAppSettings.OverheadPressTier1Max,
        Tier2MaxSetting: KnownAppSettings.OverheadPressTier2Max,
        Tier3MaxSetting: KnownAppSettings.OverheadPressTier3Max,
        Tier1Label: "Beginner",
        Tier2Label: "Intermediate",
        Tier3Label: "Advanced",
        Tier4Label: "Elite",
        Tier1DescriptionSetting: KnownAppSettings.OverheadPressTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.OverheadPressTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.OverheadPressTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.OverheadPressTier4Description);

    public static readonly MetricRatingDefinition ArmMeasurement = new(
        MetricName: "Arm Measurement",
        Tier1MaxSetting: KnownAppSettings.ArmMeasurementTier1Max,
        Tier2MaxSetting: KnownAppSettings.ArmMeasurementTier2Max,
        Tier3MaxSetting: KnownAppSettings.ArmMeasurementTier3Max,
        Tier1Label: "Average",
        Tier2Label: "Developed",
        Tier3Label: "Big",
        Tier4Label: "Exceptional",
        Tier1DescriptionSetting: KnownAppSettings.ArmMeasurementTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.ArmMeasurementTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.ArmMeasurementTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.ArmMeasurementTier4Description);

    public static readonly MetricRatingDefinition Vo2Max = new(
        MetricName: "VO2 Max",
        Tier1MaxSetting: KnownAppSettings.Vo2MaxTier1Max,
        Tier2MaxSetting: KnownAppSettings.Vo2MaxTier2Max,
        Tier3MaxSetting: KnownAppSettings.Vo2MaxTier3Max,
        Tier1Label: "Below Average",
        Tier2Label: "Average",
        Tier3Label: "Good",
        Tier4Label: "Excellent",
        Tier1DescriptionSetting: KnownAppSettings.Vo2MaxTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.Vo2MaxTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.Vo2MaxTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.Vo2MaxTier4Description);

    /// <summary>
    /// Lower is better here (a smaller waist is the healthier outcome), so
    /// HigherIsBetter is false -- Tier1Max ("Lean") is the smallest/healthiest
    /// band rather than the worst one, the reverse of every other rated
    /// metric above. See MetricRatingThresholds.HigherIsBetter.
    /// </summary>
    public static readonly MetricRatingDefinition WaistMeasurement = new(
        MetricName: "Waist Measurement",
        Tier1MaxSetting: KnownAppSettings.WaistMeasurementTier1Max,
        Tier2MaxSetting: KnownAppSettings.WaistMeasurementTier2Max,
        Tier3MaxSetting: KnownAppSettings.WaistMeasurementTier3Max,
        Tier1Label: "High",
        Tier2Label: "Elevated",
        Tier3Label: "Trim",
        Tier4Label: "Lean",
        Tier1DescriptionSetting: KnownAppSettings.WaistMeasurementTier1Description,
        Tier2DescriptionSetting: KnownAppSettings.WaistMeasurementTier2Description,
        Tier3DescriptionSetting: KnownAppSettings.WaistMeasurementTier3Description,
        Tier4DescriptionSetting: KnownAppSettings.WaistMeasurementTier4Description,
        HigherIsBetter: false);

    public static readonly IReadOnlyList<MetricRatingDefinition> All =
    [
        NetWorth, CreditScore, EmergencyFund, RetirementFund,
        StrengthTotal, Squat, BenchPress, Deadlift, OverheadPress, ArmMeasurement, Vo2Max, WaistMeasurement,
    ];

    public static MetricRatingDefinition? ForMetricName(string metricName) =>
        All.FirstOrDefault(definition => definition.MetricName == metricName);
}
