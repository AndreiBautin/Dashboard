namespace Vantage.Domain.Metrics.Evaluators;

/// <summary>Lower is better.</summary>
public sealed class DecreaseMetricEvaluator : IMetricEvaluator
{
    public EvaluationStrategy Strategy => EvaluationStrategy.Decrease;

    public MetricStatus Evaluate(IReadOnlyList<MetricSnapshot> orderedSnapshots, EvaluationConfig config)
    {
        if (orderedSnapshots.Count < 2)
        {
            return MetricStatus.InsufficientData;
        }

        var previous = orderedSnapshots[^2].Value;
        var latest = orderedSnapshots[^1].Value;

        if (latest < previous)
        {
            return MetricStatus.Improved;
        }

        return latest > previous ? MetricStatus.Regressed : MetricStatus.Stagnant;
    }
}
