using Vantage.Domain.Social;

namespace Vantage.Demo.Tests;

/// <summary>
/// Seeding has the worst failure mode in the system: getting it wrong means
/// overwriting real data. These tests approach it from the "must not destroy"
/// side rather than the "must populate" side.
/// </summary>
public class DemoSeederTests
{
    private static readonly DateOnly Today = new(2026, 8, 20);

    [Fact]
    public void FillIfEmpty_PopulatesAnEmptyStore()
    {
        var store = new DemoStore();

        var filled = DemoSeeder.FillIfEmpty(store, Today);

        Assert.True(filled);
        Assert.False(store.IsEmpty);
        Assert.NotEmpty(store.Categories);
        Assert.NotEmpty(store.Friends);
        Assert.NotEmpty(store.MonthlySnapshots);
    }

    [Fact]
    public void FillIfEmpty_LeavesAnAlreadySeededStoreCompletelyUntouched()
    {
        var store = new DemoStore();
        DemoSeeder.FillIfEmpty(store, Today);

        var before = Snapshot(store);

        var filledAgain = DemoSeeder.FillIfEmpty(store, Today);

        Assert.False(filledAgain);
        Assert.Equal(before, Snapshot(store));
    }

    [Fact]
    public void FillIfEmpty_WillNotTouchAStoreHoldingDataItDidNotSeed()
    {
        // Stands in for "this store already holds someone's real data". The
        // guarantee is that seeding refuses to run at all, whatever that data
        // happens to be — not that it merges politely.
        var store = new DemoStore();
        store.Add(new Friend("Pre-existing Record", Today.AddDays(-3), DateTimeOffset.UtcNow));

        var before = Snapshot(store);

        var filled = DemoSeeder.FillIfEmpty(store, Today);

        Assert.False(filled);
        Assert.Equal(before, Snapshot(store));
        Assert.Single(store.Friends);
    }

    [Fact]
    public void ResetAndFill_ReplacesEverythingAndReproducesTheFixtureExactly()
    {
        var store = new DemoStore();
        DemoSeeder.FillIfEmpty(store, Today);
        var original = Snapshot(store);

        store.Add(new Friend("Added After Seeding", Today, DateTimeOffset.UtcNow));
        Assert.NotEqual(original, Snapshot(store));

        DemoSeeder.ResetAndFill(store, Today);

        // Back to exactly the fixture, identities included — otherwise a
        // reset would leave the page holding ids that no longer resolve.
        Assert.Equal(original, Snapshot(store));
    }

    [Fact]
    public void SeedingIsDeterministicForAGivenToday()
    {
        var first = new DemoStore();
        var second = new DemoStore();

        DemoSeeder.FillIfEmpty(first, Today);
        DemoSeeder.FillIfEmpty(second, Today);

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Fact]
    public void SeededDatesMoveWithToday_SoTheFixtureCannotGoStale()
    {
        var thisYear = new DemoStore();
        var nextYear = new DemoStore();

        DemoSeeder.FillIfEmpty(thisYear, Today);
        DemoSeeder.FillIfEmpty(nextYear, Today.AddYears(1));

        var thisYearLatestMonth = thisYear.MonthlySnapshots.Max(s => s.Month);
        var nextYearLatestMonth = nextYear.MonthlySnapshots.Max(s => s.Month);

        // The whole window slides forward with "today". A fixture pinned to
        // absolute dates would produce the same months both times, and would
        // show a dead dashboard a year from now.
        Assert.Equal(thisYearLatestMonth.AddYears(1), nextYearLatestMonth);
        Assert.Equal(thisYear.MonthlySnapshots.Count, nextYear.MonthlySnapshots.Count);
    }

    [Fact]
    public void TheCurrentMonthIsDeliberatelyLeftUnrecorded()
    {
        var store = new DemoStore();
        DemoSeeder.FillIfEmpty(store, Today);

        var currentMonth = new DateOnly(Today.Year, Today.Month, 1);
        var thisMonth = store.MonthlySnapshots.FirstOrDefault(s => s.Month == currentMonth);

        // The demo opens in the state the monthly ritual starts from: history
        // behind you, this month still to fill in.
        Assert.Null(thisMonth);
    }

    /// <summary>
    /// A stable, comparable rendering of everything the store holds, so a test
    /// asserting "nothing changed" actually covers all of it rather than the
    /// one collection the author happened to think of.
    /// </summary>
    private static string Snapshot(DemoStore store)
    {
        var friends = store.Friends
            .Select(f => $"F:{f.Id}:{f.Name}:{f.LastHangoutDate}:{f.Notes}");
        var categories = store.Categories
            .Select(c => $"C:{c.Id}:{c.Name}:{c.SortOrder}");
        var metrics = store.MetricDefinitions
            .Select(m => $"M:{m.Id}:{m.CategoryId}:{m.Name}:{m.Unit}:{m.IsCalculated}");
        var values = store.MonthlySnapshots
            .SelectMany(s => s.MetricSnapshots, (s, ms) => $"V:{s.Month}:{ms.MetricDefinitionId}:{ms.Value}");
        var social = store.MonthlySnapshots
            .Select(s => $"S:{s.Month}:{s.SocialSnapshot?.ActiveFriendCount}");
        var keyRelationships = store.KeyRelationships
            .Select(k => $"K:{k.Id}:{k.Kind}:{k.LastContactDate}");

        return string.Join("\n", friends.Concat(categories).Concat(metrics).Concat(values).Concat(social).Concat(keyRelationships));
    }
}
