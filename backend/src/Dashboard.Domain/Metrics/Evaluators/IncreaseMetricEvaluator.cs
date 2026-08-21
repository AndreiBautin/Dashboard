namespace Dashboard.Domain.Metrics.Evaluators;

/// <summary>Higher is better — e.g. Powerlifting Total.</summary>
public sealed class IncreaseMetricEvaluator : IMetricEvaluator
{
    public EvaluationStrategy Strategy => EvaluationStrategy.Increase;

    public MetricStatus Evaluate(IReadOnlyList<MetricSnapshot> orderedSnapshots, EvaluationConfig config)
    {
        if (orderedSnapshots.Count < 2)
        {
            return MetricStatus.InsufficientData;
        }

        var previous = orderedSnapshots[^2].Value;
        var latest = orderedSnapshots[^1].Value;

        if (latest > previous)
        {
            return MetricStatus.Improved;
        }

        return latest < previous ? MetricStatus.Regressed : MetricStatus.Stagnant;
    }
}
