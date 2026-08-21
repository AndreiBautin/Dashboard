namespace Vantage.Domain.Metrics;

/// <summary>
/// How a metric's success is measured. Adding a new strategy means adding
/// one enum value and one new <see cref="IMetricEvaluator"/> implementation
/// — nothing else in the system needs to change.
/// </summary>
public enum EvaluationStrategy
{
    /// <summary>Higher is better (e.g. Powerlifting Total).</summary>
    Increase,

    /// <summary>Lower is better.</summary>
    Decrease,

    /// <summary>Must stay at or above a threshold (e.g. Credit Score).</summary>
    StayAbove,

    /// <summary>Must stay at or below a threshold.</summary>
    StayBelow,

    /// <summary>Must stay within a min/max band.</summary>
    StayWithinRange,
}
