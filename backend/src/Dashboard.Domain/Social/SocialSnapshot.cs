namespace Dashboard.Domain.Social;

/// <summary>
/// One monthly review's captured active-circle size. Captured rather than
/// derived-on-read so a friend later dropping out of the active circle
/// doesn't rewrite history — the trend reflects what the circle looked like
/// at each review, not what it would look like today if recomputed against
/// current Friend data. Belongs to exactly one
/// <see cref="Metrics.MonthlySnapshot"/>, set through
/// <see cref="Metrics.MonthlySnapshot.SetSocialSnapshot"/> rather than
/// constructed directly.
/// </summary>
public sealed class SocialSnapshot
{
    public int Id { get; private set; }
    public int MonthlySnapshotId { get; private set; }
    public int ActiveFriendCount { get; private set; }

    // For EF Core materialization only.
    private SocialSnapshot()
    {
    }

    public SocialSnapshot(int activeFriendCount)
    {
        ActiveFriendCount = activeFriendCount;
    }

    /// <summary>Called only by <see cref="Metrics.MonthlySnapshot.SetSocialSnapshot"/>.</summary>
    internal void UpdateActiveFriendCount(int activeFriendCount)
    {
        ActiveFriendCount = activeFriendCount;
    }
}
