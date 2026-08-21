using Dashboard.Domain.Social;

namespace Dashboard.Application.Social;

/// <summary>
/// One key relationship's check-in status. <paramref name="Score"/> is a
/// flat 100 (on track) or 30 (flagged overdue) -- deliberately not a
/// continuous score like Net Worth or circle size, since there's nothing
/// gradual about "have you done this or not": you either have or you
/// haven't, same flat scoring the app already uses for a metric's trend
/// status (Improved=100/Regressed=30).
/// </summary>
public sealed record KeyRelationshipSummary(
    int KeyRelationshipId,
    KeyRelationshipKind Kind,
    string Label,
    DateOnly LastContactDate,
    int DaysSinceLastContact,
    bool IsFlaggedOverdue,
    int Score);
