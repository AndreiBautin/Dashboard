using Dashboard.Application.Social;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Application.Tests.Social.Fakes;
using Dashboard.Domain.Social;

namespace Dashboard.Application.Tests.Social;

public class FriendServiceTests
{
    private readonly FakeFriendRepository _friends = new();
    private readonly FakeKeyRelationshipRepository _keyRelationships = new();
    private readonly FakeMonthlySnapshotRepository _monthlySnapshots = new();
    private readonly FakeAppSettingRepository _appSettings = new();

    private FriendService CreateService()
    {
        var socialService = new SocialService(_friends, _keyRelationships, _monthlySnapshots, _appSettings);
        return new FriendService(_friends, _monthlySnapshots, socialService, new FakeUnitOfWork());
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static DateOnly ThisMonth()
    {
        var today = Today();
        return new DateOnly(today.Year, today.Month, 1);
    }

    [Fact]
    public async Task AddFriendAsync_ReturnsTheNewFriendsId()
    {
        var service = CreateService();

        var friendId = await service.AddFriendAsync("Robin", Today(), notes: null);

        Assert.Equal(1, friendId);
    }

    [Fact]
    public async Task AddFriendAsync_RefreshesThisMonthsActiveCount()
    {
        var service = CreateService();

        await service.AddFriendAsync("Robin", Today(), notes: null);

        var thisMonthSnapshot = await _monthlySnapshots.GetByMonthAsync(ThisMonth());
        Assert.Equal(1, thisMonthSnapshot!.SocialSnapshot!.ActiveFriendCount);
    }

    [Fact]
    public async Task LogHangoutAsync_WithUnknownFriend_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.LogHangoutAsync(999, Today()));
    }

    [Fact]
    public async Task LogHangoutAsync_UpdatesTheFriendAndRefreshesTheActiveCount()
    {
        // 20 months ago is outside the default 12-month active-circle threshold.
        _friends.Seed(1, new Friend("Robin", Today().AddMonths(-20), DateTimeOffset.UtcNow));
        var service = CreateService();

        await service.LogHangoutAsync(1, Today());

        var friend = await _friends.GetByIdAsync(1);
        Assert.Equal(Today(), friend!.LastHangoutDate);

        var thisMonthSnapshot = await _monthlySnapshots.GetByMonthAsync(ThisMonth());
        Assert.Equal(1, thisMonthSnapshot!.SocialSnapshot!.ActiveFriendCount);
    }
}
