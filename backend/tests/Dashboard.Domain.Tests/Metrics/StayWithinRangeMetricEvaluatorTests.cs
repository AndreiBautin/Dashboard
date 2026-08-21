using Dashboard.Domain.Metrics;
using Dashboard.Domain.Metrics.Evaluators;

namespace Dashboard.Domain.Tests.Metrics;

public class StayWithinRangeMetricEvaluatorTests
{
    private readonly StayWithinRangeMetricEvaluator _evaluator = new();
    private static readonly EvaluationConfig RangeOf10To20 = new(MinValue: 10, MaxValue: 20);

    [Fact]
    public void Evaluate_WithFewerThanTwoSnapshots_ReturnsInsufficientData()
    {
        var snapshots = new[] { Snapshot(15) };

        var status = _evaluator.Evaluate(snapshots, RangeOf10To20);

        Assert.Equal(MetricStatus.InsufficientData, status);
    }

    [Fact]
    public void Evaluate_WithoutMinOrMax_Throws()
    {
        var snapshots = new[] { Snapshot(15), Snapshot(16) };

        Assert.Throws<InvalidOperationException>(() => _evaluator.Evaluate(snapshots, new EvaluationConfig()));
    }

    [Theory]
    [InlineData(25, 15, MetricStatus.Improved)]     // entered the range
    [InlineData(15, 16, MetricStatus.Stagnant)]     // stayed within range
    [InlineData(15, 25, MetricStatus.Regressed)]    // left the range (too high)
    [InlineData(15, 5, MetricStatus.Regressed)]     // left the range (too low)
    public void Evaluate_ComparesAgainstRange(decimal previous, decimal latest, MetricStatus expected)
    {
        var snapshots = new[] { Snapshot(previous), Snapshot(latest) };

        var status = _evaluator.Evaluate(snapshots, RangeOf10To20);

        Assert.Equal(expected, status);
    }

    private static MetricSnapshot Snapshot(decimal value) =>
        new(metricDefinitionId: 1, value, recordedAt: DateTimeOffset.UtcNow);
}
