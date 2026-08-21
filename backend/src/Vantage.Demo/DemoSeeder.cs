using Vantage.Domain.Metrics;
using Vantage.Domain.Social;

namespace Vantage.Demo;

/// <summary>
/// Turns <see cref="DemoDataset"/> into live aggregates inside a
/// <see cref="DemoStore"/>.
///
/// Note that "fill an empty store" and "throw everything away and start
/// again" are two separately named methods rather than one method with a
/// <c>bool overwrite</c> parameter. That is deliberate. A flag makes the
/// destructive case reachable by passing the wrong argument — one transposed
/// boolean at one call site and real data is gone. Two names mean a caller
/// that wants the safe operation cannot accidentally invoke the dangerous
/// one; the dangerous one has to be typed out by name.
/// </summary>
public static class DemoSeeder
{
    /// <summary>
    /// Populates the store only if it is completely empty, and reports
    /// whether it did. Safe to call on every startup: it can add data, and it
    /// can never replace or delete any.
    /// </summary>
    public static bool FillIfEmpty(DemoStore store, DateOnly today)
    {
        if (!store.IsEmpty)
        {
            return false;
        }

        Fill(store, today);
        return true;
    }

    /// <summary>
    /// Discards everything in the store and repopulates it from the fixture.
    /// Only for the demo's own "reset" affordance, where the store holds
    /// nothing but demo data by construction. Never call this against a store
    /// backed by real persistence.
    /// </summary>
    public static void ResetAndFill(DemoStore store, DateOnly today)
    {
        store.Clear();
        Fill(store, today);
    }

    private static void Fill(DemoStore store, DateOnly today)
    {
        var months = DemoDataset.HistoryMonthsFor(today);

        // Months first: every metric snapshot hangs off one of these.
        var monthlySnapshots = months
            .Select(month => store.Add(new MonthlySnapshot(month, RecordedAtFor(month))))
            .ToList();

        foreach (var demoCategory in DemoDataset.Categories)
        {
            var category = store.Add(new Category(demoCategory.Name, demoCategory.SortOrder));

            foreach (var demoMetric in demoCategory.Metrics)
            {
                var metric = store.Add(new MetricDefinition(
                    category.Id,
                    demoMetric.Name,
                    demoMetric.Unit,
                    demoMetric.Strategy,
                    demoMetric.Config,
                    demoMetric.SortOrder,
                    demoMetric.IsCalculated));

                for (var index = 0; index < demoMetric.Values.Count; index++)
                {
                    // A null means the metric genuinely was not recorded that
                    // month. Skipping it (rather than writing a zero) is what
                    // keeps "no reading" distinguishable from "a reading of
                    // nothing", which the evaluators depend on.
                    if (demoMetric.Values[index] is not { } value)
                    {
                        continue;
                    }

                    var monthlySnapshot = monthlySnapshots[index];
                    monthlySnapshot.AddMetricSnapshot(metric.Id, value, RecordedAtFor(monthlySnapshot.Month));
                }
            }
        }

        foreach (var demoFriend in DemoDataset.Friends)
        {
            store.Add(new Friend(
                demoFriend.Name,
                today.AddDays(-demoFriend.DaysSinceLastHangout),
                RecordedAtFor(months[0]),
                demoFriend.Notes));
        }

        foreach (var demoKeyRelationship in DemoDataset.KeyRelationships)
        {
            store.Add(new KeyRelationship(
                demoKeyRelationship.Kind,
                today.AddDays(-demoKeyRelationship.DaysSinceLastContact),
                RecordedAtFor(months[0])));
        }

        // The captured active-circle size per month, so Social's trend chart
        // has a line rather than a single dot. Derived from the same friend
        // offsets the friend list uses, so the two can never disagree.
        foreach (var monthlySnapshot in monthlySnapshots)
        {
            monthlySnapshot.SetSocialSnapshot(
                DemoDataset.ActiveFriendCountAt(today, monthlySnapshot.Month, DefaultActiveCircleThresholdMonths));
        }

        store.AssignPendingIds();
    }

    /// <summary>
    /// Matches <c>KnownAppSettings.ActiveCircleThresholdMonths</c>'s default.
    /// The fixture seeds no settings rows of its own — every setting resolves
    /// to its declared default — so the captured counts must be computed
    /// against that same default. Pinned by
    /// <c>DemoDatasetTests.CapturedSocialCounts_MatchTheConfiguredDefaultThreshold</c>.
    /// </summary>
    private const int DefaultActiveCircleThresholdMonths = 12;

    /// <summary>
    /// Review entries are recorded at the start of the month they describe.
    /// Only ordering metadata — evaluation orders by <c>Month</c>, never by
    /// this — but it should still be internally consistent.
    /// </summary>
    private static DateTimeOffset RecordedAtFor(DateOnly month) =>
        new(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
