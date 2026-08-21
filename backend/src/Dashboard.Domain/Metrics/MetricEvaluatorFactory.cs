namespace Dashboard.Domain.Metrics;

/// <summary>
/// Resolves the right <see cref="IMetricEvaluator"/> for a metric's
/// configured strategy. Adding a new strategy means registering one more
/// evaluator with DI (see Dashboard.Application's AddApplication) — this
/// factory needs no changes.
/// </summary>
public sealed class MetricEvaluatorFactory
{
    private readonly IReadOnlyDictionary<EvaluationStrategy, IMetricEvaluator> _evaluatorsByStrategy;

    public MetricEvaluatorFactory(IEnumerable<IMetricEvaluator> evaluators)
    {
        _evaluatorsByStrategy = evaluators.ToDictionary(evaluator => evaluator.Strategy);
    }

    public IMetricEvaluator GetEvaluator(EvaluationStrategy strategy)
    {
        if (_evaluatorsByStrategy.TryGetValue(strategy, out var evaluator))
        {
            return evaluator;
        }

        throw new InvalidOperationException($"No evaluator registered for strategy '{strategy}'.");
    }
}
