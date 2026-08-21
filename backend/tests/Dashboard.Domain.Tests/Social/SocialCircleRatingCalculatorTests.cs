using Dashboard.Domain.Social;

namespace Dashboard.Domain.Tests.Social;

public class SocialCircleRatingCalculatorTests
{
    private static readonly SocialCircleRatingThresholds DefaultThresholds = new(ThinMax: 4, HealthyMax: 8, RobustMax: 14);

    [Theory]
    [InlineData(0, SocialCircleRating.Thin)]
    [InlineData(4, SocialCircleRating.Thin)]       // top of the Thin band
    [InlineData(5, SocialCircleRating.Healthy)]    // just past it
    [InlineData(8, SocialCircleRating.Healthy)]    // top of the Healthy band
    [InlineData(9, SocialCircleRating.Robust)]     // just past it
    [InlineData(14, SocialCircleRating.Robust)]    // top of the Robust band
    [InlineData(15, SocialCircleRating.Expansive)] // just past it
    [InlineData(30, SocialCircleRating.Expansive)]
    public void Rate_WithDefaultThresholds_ReturnsTheExpectedBand(int activeFriendCount, SocialCircleRating expected)
    {
        Assert.Equal(expected, SocialCircleRatingCalculator.Rate(activeFriendCount, DefaultThresholds));
    }

    [Fact]
    public void Rate_WithCustomThresholds_HonorsThem()
    {
        var narrow = new SocialCircleRatingThresholds(ThinMax: 1, HealthyMax: 2, RobustMax: 3);

        Assert.Equal(SocialCircleRating.Thin, SocialCircleRatingCalculator.Rate(1, narrow));
        Assert.Equal(SocialCircleRating.Healthy, SocialCircleRatingCalculator.Rate(2, narrow));
        Assert.Equal(SocialCircleRating.Robust, SocialCircleRatingCalculator.Rate(3, narrow));
        Assert.Equal(SocialCircleRating.Expansive, SocialCircleRatingCalculator.Rate(4, narrow));
    }

    [Theory]
    [InlineData(0, 0)]      // bottom of the Thin band
    [InlineData(2, 12.5)]   // halfway through Thin -> halfway between 0 and 25
    [InlineData(4, 25)]     // top of Thin
    [InlineData(6, 37.5)]   // halfway through Healthy -> halfway between 25 and 50
    [InlineData(8, 50)]     // top of Healthy
    [InlineData(11, 62.5)]  // halfway through Robust -> halfway between 50 and 75
    [InlineData(14, 75)]    // top of Robust
    public void RateContinuous_InterpolatesWithinEachBand(int activeFriendCount, double expected)
    {
        var score = SocialCircleRatingCalculator.RateContinuous(activeFriendCount, DefaultThresholds);

        Assert.Equal((decimal)expected, score);
    }

    [Fact]
    public void RateContinuous_AboveRobustMax_IsAFlatOneHundred()
    {
        // 14 is still *within* Robust (interpolated, same as any other
        // band), but one more friend past it means every defined cutoff has
        // been cleared -- Expansive is "you've arrived", not a fourth band
        // to keep climbing through, so 15 and 500 score identically.
        Assert.Equal(75m, SocialCircleRatingCalculator.RateContinuous(14, DefaultThresholds));
        Assert.Equal(100m, SocialCircleRatingCalculator.RateContinuous(15, DefaultThresholds));
        Assert.Equal(100m, SocialCircleRatingCalculator.RateContinuous(20, DefaultThresholds));
        Assert.Equal(100m, SocialCircleRatingCalculator.RateContinuous(500, DefaultThresholds));
    }

    [Fact]
    public void RateContinuous_ZeroActiveFriends_IsAValidLowScore_NotNull()
    {
        // Unlike the maintenance score (which needs an active circle to
        // divide by), the size rating is always defined -- 0 friends is
        // just the bottom of the scale, not "no data".
        Assert.Equal(0m, SocialCircleRatingCalculator.RateContinuous(0, DefaultThresholds));
    }
}
