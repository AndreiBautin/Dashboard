namespace Vantage.Domain.Metrics;

/// <summary>
/// The result of evaluating a metric's most recent snapshot against its
/// evaluation strategy. This is deliberately a flat set of four outcomes —
/// every strategy, however it defines "success," maps its result onto these
/// same four values so the rest of the app (status dots, alerts, the health
/// score) never needs to know which strategy produced them.
/// </summary>
public enum MetricStatus
{
    Improved,
    Regressed,
    Stagnant,

    /// <summary>Fewer than two snapshots exist yet — nothing to compare.</summary>
    InsufficientData,
}
