namespace Dashboard.Domain.Metrics.Evaluators;

/// <summary>Must stay within a min/max band.</summary>
public sealed class StayWithinRangeMetricEvaluator : IMetricEvaluator
{
    public EvaluationStrategy Strategy => EvaluationStrategy.StayWithinRange;

    public MetricStatus Evaluate(IReadOnlyList<MetricSnapshot> orderedSnapshots, EvaluationConfig config)
    {
        if (orderedSnapshots.Count < 2)
        {
            return MetricStatus.InsufficientData;
        }

        var min = config.MinValue
            ?? throw new InvalidOperationException($"{Strategy} requires a MinValue.");
        var max = config.MaxValue
            ?? throw new InvalidOperationException($"{Strategy} requires a MaxValue.");

        var previous = orderedSnapshots[^2].Value;
        var latest = orderedSnapshots[^1].Value;

        var latestInRange = latest >= min && latest <= max;
        var previousInRange = previous >= min && previous <= max;

        if (!latestInRange)
        {
            return MetricStatus.Regressed;
        }

        return previousInRange ? MetricStatus.Stagnant : MetricStatus.Improved;
    }
}
