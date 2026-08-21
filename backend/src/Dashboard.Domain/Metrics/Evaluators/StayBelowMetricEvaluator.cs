namespace Dashboard.Domain.Metrics.Evaluators;

/// <summary>Must stay at or below a threshold.</summary>
public sealed class StayBelowMetricEvaluator : IMetricEvaluator
{
    public EvaluationStrategy Strategy => EvaluationStrategy.StayBelow;

    public MetricStatus Evaluate(IReadOnlyList<MetricSnapshot> orderedSnapshots, EvaluationConfig config)
    {
        if (orderedSnapshots.Count < 2)
        {
            return MetricStatus.InsufficientData;
        }

        var threshold = config.Threshold
            ?? throw new InvalidOperationException($"{Strategy} requires a Threshold.");

        var previous = orderedSnapshots[^2].Value;
        var latest = orderedSnapshots[^1].Value;

        if (latest > threshold)
        {
            return MetricStatus.Regressed;
        }

        return previous > threshold ? MetricStatus.Improved : MetricStatus.Stagnant;
    }
}
