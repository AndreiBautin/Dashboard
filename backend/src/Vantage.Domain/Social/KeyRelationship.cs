namespace Vantage.Domain.Social;

/// <summary>
/// Tracks the last check-in for one of the fixed <see cref="KeyRelationshipKind"/>
/// entries -- structurally almost identical to <see cref="Friend"/> (a last-contact
/// date that only ratchets forward, plus an overdue check against a configurable
/// threshold), but deliberately a separate entity rather than reusing Friend:
/// there's always exactly one row per kind (never added to or removed via the UI
/// the way Friends are), and it never counts toward active-circle size or
/// maintenance -- it's scored and alerted on independently instead.
/// </summary>
public sealed class KeyRelationship
{
    public int Id { get; private set; }
    public KeyRelationshipKind Kind { get; private set; }
    public DateOnly LastContactDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // For EF Core materialization only.
    private KeyRelationship()
    {
    }

    public KeyRelationship(KeyRelationshipKind kind, DateOnly lastContactDate, DateTimeOffset createdAt)
    {
        Kind = kind;
        LastContactDate = lastContactDate;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Records a check-in. Only advances <see cref="LastContactDate"/> forward,
    /// same ratchet behavior as <see cref="Friend.LogHangout"/> and for the
    /// same reason -- "last contact" should always reflect the most recent
    /// one known, not whichever was entered most recently.
    /// </summary>
    public void LogContact(DateOnly date)
    {
        if (date > LastContactDate)
        {
            LastContactDate = date;
        }
    }

    public int DaysSinceLastContact(DateOnly asOf) => asOf.DayNumber - LastContactDate.DayNumber;

    /// <summary>
    /// Flagged once it's been more than <paramref name="overdueThresholdMonths"/>
    /// since the last check-in. Each kind has its own configured threshold
    /// (see KnownAppSettings), rather than sharing one setting across both.
    /// </summary>
    public bool IsFlaggedOverdue(int overdueThresholdMonths, DateOnly asOf) =>
        LastContactDate.AddMonths(overdueThresholdMonths) < asOf;
}
