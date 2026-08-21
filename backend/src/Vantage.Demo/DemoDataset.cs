using Vantage.Domain.Metrics;
using Vantage.Domain.Social;

namespace Vantage.Demo;

/// <summary>
/// The one demo fixture, shared by the public WebAssembly demo and by local
/// development seeding.
///
/// Three properties are deliberate and load-bearing:
///
/// 1. <b>It is generated, never captured.</b> Every number below was written
///    by hand for a fictional person. There is no export step from anyone's
///    real database anywhere in the pipeline, so there is no path by which
///    personal data could reach it. <c>DemoDatasetPrivacyTests</c> enforces
///    this by scanning the built fixture for anything that looks like real
///    personal data.
///
/// 2. <b>Dates are relative to a supplied "today", never absolute.</b> A
///    fixture pinned to fixed timestamps rots: open it a year later and every
///    streak is dead and every "this month" panel is empty. Offsets keep it
///    alive indefinitely while staying perfectly deterministic for a given
///    <c>today</c>.
///
/// 3. <b>The current month is deliberately left blank.</b> The app's whole
///    premise is a monthly review ritual, so the demo opens in exactly the
///    state that ritual starts from — five months of history behind you, this
///    month not yet recorded. It also gives a reviewer something real to do:
///    fill in this month and watch the scores move.
/// </summary>
public static class DemoDataset
{
    /// <summary>How many months of history the fixture carries, ending with last month.</summary>
    public const int HistoryMonths = 5;

    public sealed record Metric(
        string Name,
        string Unit,
        EvaluationStrategy Strategy,
        EvaluationConfig Config,
        int SortOrder,
        bool IsCalculated,
        /// <summary>
        /// One entry per history month, oldest first, aligned to
        /// <see cref="HistoryMonths"/>. A null means "not recorded that
        /// month", which is a state the app has to handle and therefore a
        /// state the fixture has to contain.
        /// </summary>
        IReadOnlyList<decimal?> Values);

    public sealed record Category(string Name, int SortOrder, IReadOnlyList<Metric> Metrics);

    public sealed record Friend(string Name, int DaysSinceLastHangout, string? Notes);

    public sealed record KeyRelationship(KeyRelationshipKind Kind, int DaysSinceLastContact);

    // ---------------------------------------------------------------------
    // Fitness
    //
    // Spans three rating tiers so the tier indicator is never showing the
    // same segment twice, and mixes trend directions so the dashboard shows
    // improvement, a stall, and a decline rather than a wall of green.
    // ---------------------------------------------------------------------
    private static readonly Category Fitness = new("Fitness", 0,
    [
        // Steady, believable linear progress -> Improved.
        new Metric("Squat", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), 0, false,
            [225m, 235m, 245m, 260m, 275m]),

        // Lands exactly on the Intermediate/Advanced cutoff (230) in the most
        // recent month. Boundary values are where off-by-one rating bugs live,
        // so the demo keeps one permanently parked on a boundary.
        new Metric("Bench Press", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), 1, false,
            [195m, 205m, 215m, 225m, 230m]),

        // A gap in the middle: not every lift gets retested every month, and
        // the trend has to survive that.
        new Metric("Deadlift", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), 2, false,
            [295m, 315m, null, 335m, 350m]),

        // Went backwards. Drives a "has declined" alert on the dashboard.
        new Metric("Overhead Press", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), 3, false,
            [115m, 120m, 125m, 125m, 120m]),

        // Calculated: the sum of the four lifts' latest known values. Kept
        // consistent by hand here with what MetricEntryService would compute.
        new Metric("Strength Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), 4, true,
            [830m, 875m, 905m, 945m, 975m]),

        // Crosses a tier boundary mid-history (15.0 is the Tier2 cutoff), so
        // the rating label visibly changes across the trend.
        new Metric("Arm Measurement", "in", EvaluationStrategy.Increase, new EvaluationConfig(), 5, false,
            [14.4m, 14.6m, 14.9m, 15.1m, 15.3m]),

        new Metric("VO2 Max", "ml/kg/min", EvaluationStrategy.Increase, new EvaluationConfig(), 6, false,
            [43m, 44m, 45m, 46m, 47m]),

        // The one metric where smaller is better, so the fixture exercises
        // MetricRatingThresholds.HigherIsBetter = false end to end.
        new Metric("Waist Measurement", "in", EvaluationStrategy.Decrease, new EvaluationConfig(), 7, false,
            [35.5m, 35.0m, 34.5m, 34.0m, 33.5m]),
    ]);

    // ---------------------------------------------------------------------
    // Finance
    //
    // Figures for a fictional mid-career person: comfortable but plainly
    // mid-scale, so no tier reads as either broken or aspirational.
    // ---------------------------------------------------------------------
    private static readonly Category Finance = new("Finance", 1,
    [
        new Metric("Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), 0, false,
            [96_000m, 101_500m, 106_800m, 112_400m, 118_400m]),

        new Metric("Credit Score", "points", EvaluationStrategy.StayAbove, new EvaluationConfig(Threshold: 700), 1, false,
            [702m, 711m, 726m, 738m, 748m]),

        // Flat on purpose -> Stagnant -> a "has stalled" alert. An emergency
        // fund that stops growing is exactly the kind of quiet drift this app
        // exists to surface.
        new Metric("Emergency Fund", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), 2, false,
            [12_000m, 12_000m, 12_000m, 12_000m, 12_000m]),

        new Metric("Retirement Fund", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), 3, false,
            [38_000m, 40_100m, 42_400m, 44_900m, 47_500m]),
    ]);

    public static IReadOnlyList<Category> Categories => [Fitness, Finance];

    /// <summary>
    /// Six invented people whose last-hangout offsets are chosen to put one
    /// friend in each state the Social page can render: comfortably recent,
    /// recent, near the overdue edge, overdue, badly overdue, and dropped out
    /// of the active circle entirely.
    /// </summary>
    public static IReadOnlyList<Friend> Friends =>
    [
        // Minimal record: only the required fields, no notes. The UI has to
        // render a friend with nothing optional filled in.
        new("Priya", 8, null),

        new("Marcus", 26, "Climbing partner."),

        // Deliberately long note, to prove the row layout does not break on a
        // value far longer than anything a form would nudge you toward.
        new("Devon", 61,
            "Met through the old team. Always suggests the same diner on Route 9 and always orders the same thing. " +
            "Owes me a rematch after last summer, and reminds me of it more often than I do."),

        // Past the 3-month overdue threshold but still inside the 12-month
        // active window -> flagged, still counted.
        new("Ingrid", 118, "Was in town for a conference."),

        new("Tomas", 200, "Moved away last year."),

        // Outside the active-circle window entirely -> inactive. Demonstrates
        // that dropping out of the circle never deletes the record.
        new("Nadia", 402, "Lost touch after the move."),
    ];

    /// <summary>
    /// One inside its threshold, one outside it, so the dashboard shows both
    /// a healthy key relationship and an overdue one raising an alert.
    /// </summary>
    public static IReadOnlyList<KeyRelationship> KeyRelationships =>
    [
        new(KeyRelationshipKind.DateWithWife, 12),
        new(KeyRelationshipKind.VisitedMother, 45),
    ];

    /// <summary>
    /// The review months the fixture covers, oldest first: the five months
    /// ending with last month. The current month is intentionally absent —
    /// see the note on the class.
    /// </summary>
    public static IReadOnlyList<DateOnly> HistoryMonthsFor(DateOnly today)
    {
        var thisMonth = new DateOnly(today.Year, today.Month, 1);
        return Enumerable.Range(0, HistoryMonths)
            .Select(index => thisMonth.AddMonths(-(HistoryMonths - index)))
            .ToList();
    }

    /// <summary>
    /// The active-circle size as it stood at each history month, used to give
    /// the Social trend chart a history rather than a single point. Derived
    /// from the friend offsets themselves rather than hand-written, so it can
    /// never contradict the friend list.
    /// </summary>
    public static int ActiveFriendCountAt(DateOnly today, DateOnly month, int activeCircleThresholdMonths)
    {
        // A friend counted toward the circle at `month` if their hangout had
        // already happened by then and was still inside the active window.
        var asOf = month.AddMonths(1).AddDays(-1);
        if (asOf > today)
        {
            asOf = today;
        }

        return Friends.Count(friend =>
        {
            var lastHangout = today.AddDays(-friend.DaysSinceLastHangout);
            return lastHangout <= asOf && lastHangout.AddMonths(activeCircleThresholdMonths) >= asOf;
        });
    }
}
