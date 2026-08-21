namespace Vantage.Domain.Metrics;

/// <summary>
/// Strategy-specific parameters for a <see cref="MetricDefinition"/>'s evaluation.
/// Each <see cref="IMetricEvaluator"/> reads only the fields it needs — e.g.
/// <see cref="Threshold"/> for StayAbove/StayBelow, <see cref="MinValue"/> and
/// <see cref="MaxValue"/> for StayWithinRange. Increase/Decrease need none of
/// these fields at all.
///
/// Kept as a small, strongly-typed record rather than a raw JSON string so the
/// Domain layer never has to think about serialization — that concern belongs
/// entirely to Infrastructure's EF Core value converter.
/// </summary>
public sealed record EvaluationConfig(
    decimal? Threshold = null,
    decimal? MinValue = null,
    decimal? MaxValue = null);
