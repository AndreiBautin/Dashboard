namespace Vantage.Application.Social;

public sealed record FriendSummary(
    int FriendId,
    string Name,
    string? Notes,
    DateOnly LastHangoutDate,
    int DaysSinceLastHangout,
    bool IsActive,
    bool IsFlaggedOverdue);
