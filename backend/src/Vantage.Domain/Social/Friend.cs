namespace Vantage.Domain.Social;

/// <summary>
/// One person in your circle. Never hard-deleted: dropping out of the
/// active circle only affects <see cref="IsActive"/> (derived at query
/// time from <see cref="LastHangoutDate"/>), not this record's existence --
/// history and the record itself persist even after someone goes inactive,
/// and they simply become active again if <see cref="LogHangout"/> is
/// called with a recent enough date.
/// </summary>
public sealed class Friend
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateOnly LastHangoutDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // For EF Core materialization only.
    private Friend()
    {
    }

    public Friend(string name, DateOnly lastHangoutDate, DateTimeOffset createdAt, string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Friend name is required.", nameof(name));
        }

        Name = name;
        LastHangoutDate = lastHangoutDate;
        CreatedAt = createdAt;
        Notes = notes;
    }

    /// <summary>
    /// Records a hangout. Only advances <see cref="LastHangoutDate"/> forward
    /// -- logging an older date than what's already on file is a no-op,
    /// since "last hangout" should always reflect the most recent one known,
    /// not whichever was entered most recently.
    /// </summary>
    public void LogHangout(DateOnly date)
    {
        if (date > LastHangoutDate)
        {
            LastHangoutDate = date;
        }
    }

    public int DaysSinceLastHangout(DateOnly asOf) => asOf.DayNumber - LastHangoutDate.DayNumber;

    /// <summary>
    /// Whether this friend counts toward the active circle: hung out with
    /// within <paramref name="activeCircleThresholdMonths"/> of
    /// <paramref name="asOf"/>. The threshold is configurable (see
    /// <c>AppSetting</c>) rather than hardcoded, defaulting to 12 months.
    /// </summary>
    public bool IsActive(int activeCircleThresholdMonths, DateOnly asOf) =>
        LastHangoutDate.AddMonths(activeCircleThresholdMonths) >= asOf;

    /// <summary>
    /// Flagged once it's been more than <paramref name="overdueThresholdMonths"/>
    /// since the last hangout. Configurable (see <c>AppSetting</c>) rather
    /// than hardcoded, same rationale as <see cref="IsActive"/>'s threshold,
    /// defaulting to 3 months.
    /// </summary>
    public bool IsFlaggedOverdue(int overdueThresholdMonths, DateOnly asOf) =>
        LastHangoutDate.AddMonths(overdueThresholdMonths) < asOf;
}
