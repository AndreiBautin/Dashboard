using Vantage.Domain.Metrics;

namespace Vantage.Domain.Tests.Metrics;

public class MetricRatingCalculatorTests
{
    private static readonly MetricRatingThresholds Thresholds = new(Tier1Max: 100, Tier2Max: 200, Tier3Max: 300);

    // int InlineData values, widened to decimal inside the test body -- xUnit's
    // attribute/reflection boundary can't carry decimal literals directly
    // (decimal isn't a valid custom-attribute argument type), so the theory
    // parameter stays int and the implicit int-to-decimal conversion happens
    // in ordinary compiled code when calling Rate.
    [Theory]
    [InlineData(0, MetricRatingTier.Tier1)]
    [InlineData(100, MetricRatingTier.Tier1)]  // top of Tier1
    [InlineData(101, MetricRatingTier.Tier2)]  // just past it
    [InlineData(200, MetricRatingTier.Tier2)]  // top of Tier2
    [InlineData(201, MetricRatingTier.Tier3)]  // just past it
    [InlineData(300, MetricRatingTier.Tier3)]  // top of Tier3
    [InlineData(301, MetricRatingTier.Tier4)]  // just past it
    [InlineData(10_000, MetricRatingTier.Tier4)]
    public void Rate_ReturnsTheExpectedTier(int value, MetricRatingTier expected)
    {
        Assert.Equal(expected, MetricRatingCalculator.Rate(value, Thresholds));
    }

    [Fact]
    public void Rate_HonorsFractionalValuesAndThresholds()
    {
        var thresholds = new MetricRatingThresholds(Tier1Max: 13.5m, Tier2Max: 15.5m, Tier3Max: 17.5m);

        Assert.Equal(MetricRatingTier.Tier1, MetricRatingCalculator.Rate(13.5m, thresholds));
        Assert.Equal(MetricRatingTier.Tier2, MetricRatingCalculator.Rate(13.6m, thresholds));
        Assert.Equal(MetricRatingTier.Tier4, MetricRatingCalculator.Rate(20m, thresholds));
    }

    [Theory]
    [InlineData(0, 0)]        // bottom of Tier1's floor
    [InlineData(50, 12.5)]    // halfway through Tier1 -> halfway between 0 and 25
    [InlineData(100, 25)]     // top of Tier1
    [InlineData(150, 37.5)]   // halfway through Tier2 -> halfway between 25 and 50
    [InlineData(200, 50)]     // top of Tier2
    [InlineData(250, 62.5)]   // halfway through Tier3 -> halfway between 50 and 75
    [InlineData(300, 75)]     // top of Tier3
    public void RateContinuous_InterpolatesWithinEachBand(int value, double expected)
    {
        var score = MetricRatingCalculator.RateContinuous(value, Thresholds);

        Assert.Equal((decimal)expected, score);
    }

    [Fact]
    public void RateContinuous_TwoValuesInTheSameTierGetDifferentScores()
    {
        // Both land in Tier2 (101-200), but 105 is barely past the Tier1
        // boundary while 195 is nearly at Tier3 -- the whole point of this
        // method is that they shouldn't be flattened to the same number the
        // way Rate()'s coarse Tier2 answer would.
        var barelyIn = MetricRatingCalculator.RateContinuous(105, Thresholds);
        var almostOut = MetricRatingCalculator.RateContinuous(195, Thresholds);

        Assert.True(almostOut > barelyIn);
    }

    [Fact]
    public void RateContinuous_AboveTier3Max_IsAFlatOneHundred()
    {
        // 300 is still *within* Tier3 (interpolated, same as any other
        // band), but so much as $1 past it means every defined cutoff has
        // been cleared -- Tier4 is "you've arrived", not a fourth band to
        // keep climbing through, so 301 and 10,000 score identically.
        Assert.Equal(75m, MetricRatingCalculator.RateContinuous(300, Thresholds));
        Assert.Equal(100m, MetricRatingCalculator.RateContinuous(301, Thresholds));
        Assert.Equal(100m, MetricRatingCalculator.RateContinuous(400, Thresholds));
        Assert.Equal(100m, MetricRatingCalculator.RateContinuous(10_000, Thresholds));
    }

    [Fact]
    public void RateContinuous_NeverGoesBelowZeroOrAboveOneHundred()
    {
        Assert.Equal(0m, MetricRatingCalculator.RateContinuous(-500, Thresholds));
        Assert.Equal(100m, MetricRatingCalculator.RateContinuous(1_000_000, Thresholds));
    }

    // Waist Measurement is the motivating case: a smaller value is the
    // *better* outcome, the reverse of every other rated metric (Net Worth,
    // Arm Measurement, etc.). HigherIsBetter: false mirrors Tier1/Tier4 (and
    // the continuous score) around the same ascending thresholds rather than
    // requiring a whole separate threshold shape.
    private static readonly MetricRatingThresholds LowerIsBetterThresholds =
        new(Tier1Max: 100, Tier2Max: 200, Tier3Max: 300, HigherIsBetter: false);

    [Theory]
    [InlineData(0, MetricRatingTier.Tier4)]    // smallest values -> best tier
    [InlineData(100, MetricRatingTier.Tier4)]  // top of the "best" band
    [InlineData(101, MetricRatingTier.Tier3)]
    [InlineData(200, MetricRatingTier.Tier3)]
    [InlineData(201, MetricRatingTier.Tier2)]
    [InlineData(300, MetricRatingTier.Tier2)]
    [InlineData(301, MetricRatingTier.Tier1)]  // just past it -> worst tier
    [InlineData(10_000, MetricRatingTier.Tier1)]
    public void Rate_WhenLowerIsBetter_InvertsTheTierAssignment(int value, MetricRatingTier expected)
    {
        Assert.Equal(expected, MetricRatingCalculator.Rate(value, LowerIsBetterThresholds));
    }

    [Theory]
    [InlineData(0, 100)]      // smallest value -> perfect score
    [InlineData(100, 75)]     // top of the "best" band
    [InlineData(200, 50)]
    [InlineData(300, 25)]     // top of the "worst" ascending band
    [InlineData(301, 0)]      // just past it -> the worst possible score, flat
    [InlineData(10_000, 0)]
    public void RateContinuous_WhenLowerIsBetter_MirrorsTheAscendingScore(int value, double expected)
    {
        var score = MetricRatingCalculator.RateContinuous(value, LowerIsBetterThresholds);

        Assert.Equal((decimal)expected, score);
    }
}
