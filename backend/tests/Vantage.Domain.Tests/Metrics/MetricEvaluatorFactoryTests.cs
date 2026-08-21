using Vantage.Domain.Metrics;
using Vantage.Domain.Metrics.Evaluators;

namespace Vantage.Domain.Tests.Metrics;

public class MetricEvaluatorFactoryTests
{
    private static MetricEvaluatorFactory CreateFactory() => new(
    [
        new IncreaseMetricEvaluator(),
        new DecreaseMetricEvaluator(),
        new StayAboveMetricEvaluator(),
        new StayBelowMetricEvaluator(),
        new StayWithinRangeMetricEvaluator(),
    ]);

    [Theory]
    [InlineData(EvaluationStrategy.Increase, typeof(IncreaseMetricEvaluator))]
    [InlineData(EvaluationStrategy.Decrease, typeof(DecreaseMetricEvaluator))]
    [InlineData(EvaluationStrategy.StayAbove, typeof(StayAboveMetricEvaluator))]
    [InlineData(EvaluationStrategy.StayBelow, typeof(StayBelowMetricEvaluator))]
    [InlineData(EvaluationStrategy.StayWithinRange, typeof(StayWithinRangeMetricEvaluator))]
    public void GetEvaluator_ReturnsTheMatchingEvaluator(EvaluationStrategy strategy, Type expectedType)
    {
        var factory = CreateFactory();

        var evaluator = factory.GetEvaluator(strategy);

        Assert.IsType(expectedType, evaluator);
    }

    [Fact]
    public void GetEvaluator_WithNoEvaluatorsRegistered_Throws()
    {
        var factory = new MetricEvaluatorFactory([]);

        Assert.Throws<InvalidOperationException>(() => factory.GetEvaluator(EvaluationStrategy.Increase));
    }
}
