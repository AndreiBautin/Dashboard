using Vantage.Application.Metrics;
using Vantage.Domain.Metrics;
using Vantage.Domain.Social;

namespace Vantage.Application.Social;

/// <summary>
/// The write side for Friends: adding someone new, or logging a hangout
/// with an existing one. Either action can change who's in the active
/// circle, so both refresh this month's captured SocialSnapshot count
/// afterward -- callers never have to remember to do that separately.
/// </summary>
public sealed class FriendService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IMonthlySnapshotRepository _monthlySnapshotRepository;
    private readonly SocialService _socialService;
    private readonly IUnitOfWork _unitOfWork;

    public FriendService(
        IFriendRepository friendRepository,
        IMonthlySnapshotRepository monthlySnapshotRepository,
        SocialService socialService,
        IUnitOfWork unitOfWork)
    {
        _friendRepository = friendRepository;
        _monthlySnapshotRepository = monthlySnapshotRepository;
        _socialService = socialService;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> AddFriendAsync(
        string name, DateOnly lastHangoutDate, string? notes, CancellationToken cancellationToken = default)
    {
        var friend = new Friend(name, lastHangoutDate, DateTimeOffset.UtcNow, notes);
        await _friendRepository.AddAsync(friend, cancellationToken);

        await RefreshThisMonthsActiveCountAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return friend.Id;
    }

    public async Task LogHangoutAsync(int friendId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var friend = await _friendRepository.GetByIdAsync(friendId, cancellationToken)
            ?? throw new InvalidOperationException($"Friend {friendId} was not found.");

        friend.LogHangout(date);

        await RefreshThisMonthsActiveCountAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RefreshThisMonthsActiveCountAsync(CancellationToken cancellationToken)
    {
        var thresholdMonths = await _socialService.GetActiveCircleThresholdMonthsAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var friends = await _friendRepository.GetAllAsync(cancellationToken);
        var activeCount = friends.Count(friend => friend.IsActive(thresholdMonths, today));

        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var monthlySnapshot = await _monthlySnapshotRepository.GetByMonthAsync(currentMonth, cancellationToken);
        if (monthlySnapshot is null)
        {
            monthlySnapshot = new MonthlySnapshot(currentMonth, DateTimeOffset.UtcNow);
            await _monthlySnapshotRepository.AddAsync(monthlySnapshot, cancellationToken);
        }

        monthlySnapshot.SetSocialSnapshot(activeCount);
    }
}
