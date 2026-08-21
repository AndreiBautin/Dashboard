namespace Dashboard.Api.Contracts;

public sealed record AddFriendRequest(string Name, DateOnly LastHangoutDate, string? Notes);
