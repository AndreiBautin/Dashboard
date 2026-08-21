using Dashboard.Domain.Social;

namespace Dashboard.Domain.Tests.Social;

public class FriendTests
{
    [Fact]
    public void Constructor_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Friend(" ", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void LogHangout_WithALaterDate_AdvancesLastHangoutDate()
    {
        var friend = new Friend("Robin", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        friend.LogHangout(new DateOnly(2026, 6, 1));

        Assert.Equal(new DateOnly(2026, 6, 1), friend.LastHangoutDate);
    }

    [Fact]
    public void LogHangout_WithAnEarlierDate_IsANoOp()
    {
        var friend = new Friend("Robin", new DateOnly(2026, 6, 1), DateTimeOffset.UtcNow);

        friend.LogHangout(new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 6, 1), friend.LastHangoutDate);
    }

    [Fact]
    public void DaysSinceLastHangout_ReturnsTheElapsedDays()
    {
        var friend = new Friend("Robin", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        var days = friend.DaysSinceLastHangout(new DateOnly(2026, 1, 15));

        Assert.Equal(14, days);
    }

    [Theory]
    [InlineData(12, "2026-12-31", true)]  // exactly at the 12-month boundary -- still active
    [InlineData(12, "2027-01-02", false)] // just past the boundary -- no longer active
    public void IsActive_ReflectsWhetherLastHangoutIsWithinTheThreshold(int thresholdMonths, string asOf, bool expected)
    {
        var friend = new Friend("Robin", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        Assert.Equal(expected, friend.IsActive(thresholdMonths, DateOnly.Parse(asOf)));
    }

    [Theory]
    [InlineData(3, "2026-04-01", false)] // exactly at the threshold -- not yet flagged
    [InlineData(3, "2026-04-02", true)]  // just past it -- flagged
    [InlineData(1, "2026-02-02", true)]  // proves a configured threshold is honored, not a hardcoded 3
    public void IsFlaggedOverdue_ReflectsWhetherLastHangoutIsPastTheThreshold(int thresholdMonths, string asOf, bool expected)
    {
        var friend = new Friend("Robin", new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        Assert.Equal(expected, friend.IsFlaggedOverdue(thresholdMonths, DateOnly.Parse(asOf)));
    }
}
