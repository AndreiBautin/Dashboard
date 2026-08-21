using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Metrics;

/// <summary>
/// Answers "how is this metric doing?" by loading its snapshot history and
/// handing it to the evaluator matching its configured strategy.
/// </summary>
public sealed class MetricEvaluationService
{
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricSnapshotRepository _metricSnapshotRepository;
    private readonly MetricEvaluatorFactory _evaluatorFactory;

    public MetricEvaluationService(
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricSnapshotRepository metricSnapshotRepository,
        MetricEvaluatorFactory evaluatorFactory)
    {
        _metricDefinitionRepository = metricDefinitionRepository;
        _metricSnapshotRepository = metricSnapshotRepository;
        _evaluatorFactory = evaluatorFactory;
    }

    public async Task<MetricStatus> EvaluateAsync(int metricDefinitionId, CancellationToken cancellationToken = default)
    {
        var metricDefinition = await _metricDefinitionRepository.GetByIdAsync(metricDefinitionId, cancellationToken)
            ?? throw new InvalidOperationException($"Metric definition {metricDefinitionId} was not found.");

        var snapshots = await _metricSnapshotRepository.GetForMetricAsync(metricDefinitionId, cancellationToken);

        var evaluator = _evaluatorFactory.GetEvaluator(metricDefinition.EvaluationStrategy);

        return evaluator.Evaluate(snapshots, metricDefinition.EvaluationConfig);
    }
}
