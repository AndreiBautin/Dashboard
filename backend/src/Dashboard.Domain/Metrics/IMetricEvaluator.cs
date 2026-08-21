namespace Dashboard.Domain.Metrics;

/// <summary>
/// Turns a metric's snapshot history into a <see cref="MetricStatus"/>. One
/// implementation per <see cref="EvaluationStrategy"/> — pure logic, no I/O,
/// so every evaluator is trivially unit-testable in isolation.
/// </summary>
public interface IMetricEvaluator
{
    EvaluationStrategy Strategy { get; }

    /// <summary>
    /// Evaluates the most recent snapshot against the one before it.
    /// </summary>
    /// <param name="orderedSnapshots">
    /// Snapshots for a single metric, ordered oldest to newest.
    /// </param>
    /// <param name="config">The metric definition's evaluation parameters.</param>
    MetricStatus Evaluate(IReadOnlyList<MetricSnapshot> orderedSnapshots, EvaluationConfig config);
}
