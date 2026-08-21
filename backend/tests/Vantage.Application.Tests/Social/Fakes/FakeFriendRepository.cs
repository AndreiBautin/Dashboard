using Vantage.Application.Social;
using Vantage.Domain.Social;

namespace Vantage.Application.Tests.Social.Fakes;

public sealed class FakeFriendRepository : IFriendRepository
{
    private static readonly System.Reflection.PropertyInfo IdProperty =
        typeof(Friend).GetProperty(nameof(Friend.Id))!;

    private readonly Dictionary<int, Friend> _friendsById = new();

    /// <summary>Assigns the given id via reflection, same rationale as FakeCategoryRepository.Seed.</summary>
    public void Seed(int id, Friend friend)
    {
        IdProperty.SetValue(friend, id);
        _friendsById[id] = friend;
    }

    public Task<IReadOnlyList<Friend>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Friend>>(_friendsById.Values.ToList());

    public Task<Friend?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_friendsById.GetValueOrDefault(id));

    /// <summary>
    /// Unlike FakeCategoryRepository/FakeMetricDefinitionRepository's AddAsync
    /// (which don't bother assigning Id, since nothing in production code
    /// reads it right after adding), FriendService.AddFriendAsync returns the
    /// new friend's Id immediately after adding -- so this fake has to
    /// simulate EF's auto-increment behavior for that to work in tests.
    /// </summary>
    public Task AddAsync(Friend friend, CancellationToken cancellationToken = default)
    {
        var nextId = _friendsById.Count == 0 ? 1 : _friendsById.Keys.Max() + 1;
        IdProperty.SetValue(friend, nextId);
        _friendsById[nextId] = friend;
        return Task.CompletedTask;
    }
}
