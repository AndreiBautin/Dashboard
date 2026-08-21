using Vantage.Domain.Social;

namespace Vantage.Domain.Tests.Social;

public class KeyRelationshipTests
{
    [Fact]
    public void LogContact_WithALaterDate_AdvancesLastContactDate()
    {
        var relationship = new KeyRelationship(KeyRelationshipKind.DateWithWife, new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        relationship.LogContact(new DateOnly(2026, 6, 1));

        Assert.Equal(new DateOnly(2026, 6, 1), relationship.LastContactDate);
    }

    [Fact]
    public void LogContact_WithAnEarlierDate_IsANoOp()
    {
        var relationship = new KeyRelationship(KeyRelationshipKind.VisitedMother, new DateOnly(2026, 6, 1), DateTimeOffset.UtcNow);

        relationship.LogContact(new DateOnly(2026, 1, 1));

        Assert.Equal(new DateOnly(2026, 6, 1), relationship.LastContactDate);
    }

    [Fact]
    public void DaysSinceLastContact_ReturnsTheElapsedDays()
    {
        var relationship = new KeyRelationship(KeyRelationshipKind.DateWithWife, new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        var days = relationship.DaysSinceLastContact(new DateOnly(2026, 1, 15));

        Assert.Equal(14, days);
    }

    [Theory]
    [InlineData(1, "2026-02-01", false)] // exactly at the threshold -- not yet flagged
    [InlineData(1, "2026-02-02", true)]  // just past it -- flagged
    [InlineData(2, "2026-02-15", false)] // proves a configured threshold is honored, not a hardcoded 1
    public void IsFlaggedOverdue_ReflectsWhetherLastContactIsPastTheThreshold(int thresholdMonths, string asOf, bool expected)
    {
        var relationship = new KeyRelationship(KeyRelationshipKind.VisitedMother, new DateOnly(2026, 1, 1), DateTimeOffset.UtcNow);

        Assert.Equal(expected, relationship.IsFlaggedOverdue(thresholdMonths, DateOnly.Parse(asOf)));
    }
}
