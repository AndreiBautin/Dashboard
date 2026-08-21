using Vantage.Domain.Metrics;

namespace Vantage.Domain.Tests.Metrics;

public class MetricDefinitionTests
{
    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricDefinition(categoryId: 1, name: "  ", unit: "kg", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
    }

    [Fact]
    public void Constructor_WithBlankUnit_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricDefinition(categoryId: 1, name: "Powerlifting Total", unit: " ", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
    }

    [Theory]
    [InlineData(EvaluationStrategy.StayAbove)]
    [InlineData(EvaluationStrategy.StayBelow)]
    public void Constructor_StayAboveOrBelow_WithoutThreshold_Throws(EvaluationStrategy strategy)
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricDefinition(categoryId: 1, name: "Credit Score", unit: "points", strategy, new EvaluationConfig(), sortOrder: 0));
    }

    [Fact]
    public void Constructor_StayWithinRange_WithoutMinOrMax_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricDefinition(categoryId: 1, name: "Body Fat %", unit: "%", EvaluationStrategy.StayWithinRange, new EvaluationConfig(), sortOrder: 0));
    }

    [Fact]
    public void Constructor_StayWithinRange_WithMinNotLessThanMax_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new MetricDefinition(
                categoryId: 1, name: "Body Fat %", unit: "%", EvaluationStrategy.StayWithinRange,
                new EvaluationConfig(MinValue: 20, MaxValue: 10), sortOrder: 0));
    }

    [Fact]
    public void Constructor_WithValidData_CreatesAnActiveMetric()
    {
        var metric = new MetricDefinition(
            categoryId: 1, name: "Powerlifting Total", unit: "lb",
            EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);

        Assert.True(metric.IsActive);
        Assert.Equal("Powerlifting Total", metric.Name);
    }

    [Fact]
    public void Constructor_WithoutIsCalculated_DefaultsToFalse()
    {
        var metric = new MetricDefinition(
            categoryId: 1, name: "Squat", unit: "lb",
            EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);

        Assert.False(metric.IsCalculated);
    }

    [Fact]
    public void Constructor_WithIsCalculatedTrue_MarksItCalculated()
    {
        var metric = new MetricDefinition(
            categoryId: 1, name: "Strength Total", unit: "lb",
            EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 4, isCalculated: true);

        Assert.True(metric.IsCalculated);
    }
}
