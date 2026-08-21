using Microsoft.EntityFrameworkCore;
using Dashboard.Demo;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Social;

namespace Dashboard.Infrastructure.Persistence;

/// <summary>
/// Fills a brand-new local database with the same fixture the public demo
/// uses, so a fresh clone has something to render.
///
/// Two things about this are deliberate and worth knowing before changing it.
///
/// <b>It shares one fixture with the demo.</b> The data comes from
/// <see cref="DemoDataset"/> rather than from literals here, so there is
/// exactly one description of the sample data in the repository. A local
/// database and the deployed demo therefore cannot show different things, and
/// the privacy tests that scan the fixture cover this path too.
///
/// <b>It only ever fills an empty database.</b> Earlier versions of this file
/// were not really a seeder: they were a sequence of marker-gated one-time
/// migrations that deleted rows (<c>MetricSnapshots.RemoveRange(...)</c>) and
/// wrote real balances over them. Those steps have long since run wherever
/// they were going to run, and they had no business executing automatically
/// at startup. What replaced them is the single guard below — if anything at
/// all is already stored, this does nothing.
/// </summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(DashboardDbContext dbContext, CancellationToken cancellationToken = default)
    {
        // One guard, checked against the table every other table hangs off.
        // Not per-section gating: a partially seeded database is a state
        // nothing here can reason about safely, and refusing is the only
        // answer that cannot destroy anything.
        if (await dbContext.Categories.AnyAsync(cancellationToken))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var months = DemoDataset.HistoryMonthsFor(today);

        var monthlySnapshots = new List<MonthlySnapshot>();
        foreach (var month in months)
        {
            var snapshot = new MonthlySnapshot(month, RecordedAtFor(month));
            dbContext.MonthlySnapshots.Add(snapshot);
            monthlySnapshots.Add(snapshot);
        }

        // Saved before the metric values are attached: EF assigns the metric
        // definition keys here, and AddMetricSnapshot needs them.
        foreach (var demoCategory in DemoDataset.Categories)
        {
            var category = new Category(demoCategory.Name, demoCategory.SortOrder);
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var demoMetric in demoCategory.Metrics)
            {
                var metric = new MetricDefinition(
                    category.Id,
                    demoMetric.Name,
                    demoMetric.Unit,
                    demoMetric.Strategy,
                    demoMetric.Config,
                    demoMetric.SortOrder,
                    demoMetric.IsCalculated);
                dbContext.MetricDefinitions.Add(metric);
                await dbContext.SaveChangesAsync(cancellationToken);

                for (var index = 0; index < demoMetric.Values.Count; index++)
                {
                    // A null means the metric genuinely was not recorded that
                    // month. Skipping it rather than writing a zero is what
                    // keeps "no reading" distinct from "a reading of nothing".
                    if (demoMetric.Values[index] is not { } value)
                    {
                        continue;
                    }

                    monthlySnapshots[index].AddMetricSnapshot(metric.Id, value, RecordedAtFor(months[index]));
                }
            }
        }

        foreach (var demoFriend in DemoDataset.Friends)
        {
            dbContext.Friends.Add(new Friend(
                demoFriend.Name,
                today.AddDays(-demoFriend.DaysSinceLastHangout),
                RecordedAtFor(months[0]),
                demoFriend.Notes));
        }

        foreach (var demoKeyRelationship in DemoDataset.KeyRelationships)
        {
            dbContext.KeyRelationships.Add(new KeyRelationship(
                demoKeyRelationship.Kind,
                today.AddDays(-demoKeyRelationship.DaysSinceLastContact),
                RecordedAtFor(months[0])));
        }

        foreach (var monthlySnapshot in monthlySnapshots)
        {
            monthlySnapshot.SetSocialSnapshot(
                DemoDataset.ActiveFriendCountAt(today, monthlySnapshot.Month, DefaultActiveCircleThresholdMonths));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Matches <c>KnownAppSettings.ActiveCircleThresholdMonths</c>'s default.</summary>
    private const int DefaultActiveCircleThresholdMonths = 12;

    private static DateTimeOffset RecordedAtFor(DateOnly month) =>
        new(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
