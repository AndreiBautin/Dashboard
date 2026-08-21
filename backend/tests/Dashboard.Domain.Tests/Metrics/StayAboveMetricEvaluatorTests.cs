using Dashboard.Domain.Metrics;
using Dashboard.Domain.Metrics.Evaluators;

namespace Dashboard.Domain.Tests.Metrics;

public class StayAboveMetricEvaluatorTests
{
    private readonly StayAboveMetricEvaluator _evaluator = new();
    private static readonly EvaluationConfig ThresholdOf700 = new(Threshold: 700);

    [Fact]
    public void Evaluate_WithFewerThanTwoSnapshots_ReturnsInsufficientData()
    {
        var snapshots = new[] { Snapshot(720) };

        var status = _evaluator.Evaluate(snapshots, ThresholdOf700);

        Assert.Equal(MetricStatus.InsufficientData, status);
    }

    [Fact]
    public void Evaluate_WithoutThreshold_Throws()
    {
        var snapshots = new[] { Snapshot(720), Snapshot(710) };

        Assert.Throws<InvalidOperationException>(() => _evaluator.Evaluate(snapshots, new EvaluationConfig()));
    }

    [Theory]
    [InlineData(650, 720, MetricStatus.Improved)]      // crossed back above the threshold
    [InlineData(720, 710, MetricStatus.Stagnant)]      // stayed above, nothing new to report
    [InlineData(720, 650, MetricStatus.Regressed)]     // fell below the threshold
    [InlineData(650, 650, MetricStatus.Regressed)]     // stayed below
    public void Evaluate_ComparesAgainstThreshold(decimal previous, decimal latest, MetricStatus expected)
    {
        var snapshots = new[] { Snapshot(previous), Snapshot(latest) };

        var status = _evaluator.Evaluate(snapshots, ThresholdOf700);

        Assert.Equal(expected, status);
    }

    private static MetricSnapshot Snapshot(decimal value) =>
        new(metricDefinitionId: 1, value, recordedAt: DateTimeOffset.UtcNow);
}
