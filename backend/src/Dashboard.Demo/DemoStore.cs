using Dashboard.Application.Metrics;
using Dashboard.Application.Settings;
using Dashboard.Application.Social;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Settings;
using Dashboard.Domain.Social;

namespace Dashboard.Demo;

/// <summary>
/// An in-memory stand-in for the database, holding the same aggregates EF
/// Core would materialize.
///
/// This is the entire reason the public demo can run the real application
/// logic instead of a reimplementation of it: persistence in this codebase is
/// already expressed as seven small interfaces on the Application layer, so
/// swapping PostgreSQL for a few <see cref="List{T}"/>s is a matter of
/// implementing those interfaces, not of rewriting anything above them.
/// <see cref="DashboardService"/> cannot tell the difference.
/// </summary>
public sealed class DemoStore
{
    private int _nextCategoryId = 1;
    private int _nextMetricDefinitionId = 1;
    private int _nextMonthlySnapshotId = 1;
    private int _nextMetricSnapshotId = 1;
    private int _nextFriendId = 1;
    private int _nextKeyRelationshipId = 1;
    private int _nextSocialSnapshotId = 1;

    internal List<Category> Categories { get; } = [];
    internal List<MetricDefinition> MetricDefinitions { get; } = [];
    internal List<MonthlySnapshot> MonthlySnapshots { get; } = [];
    internal List<Friend> Friends { get; } = [];
    internal List<KeyRelationship> KeyRelationships { get; } = [];
    internal List<AppSetting> AppSettings { get; } = [];

    /// <summary>
    /// Counts, exposed for the demo host's startup log and its deployment
    /// smoke test. Deliberately counts rather than the collections
    /// themselves: nothing outside this assembly has any business holding a
    /// reference to the live aggregates.
    /// </summary>
    public int CategoryCount => Categories.Count;

    public int MetricDefinitionCount => MetricDefinitions.Count;

    public int MonthCount => MonthlySnapshots.Count;

    public int FriendCount => Friends.Count;

    /// <summary>
    /// True once anything at all has been stored. <see cref="DemoSeeder"/>
    /// reads this to guarantee it only ever fills an empty store.
    /// </summary>
    public bool IsEmpty =>
        Categories.Count == 0
        && MetricDefinitions.Count == 0
        && MonthlySnapshots.Count == 0
        && Friends.Count == 0
        && KeyRelationships.Count == 0;

    /// <summary>
    /// Empties the store and rewinds every identity counter, so a reset
    /// produces exactly the same ids as a first fill — otherwise a demo
    /// reset would invalidate any id the page was already holding.
    /// Reachable only through <see cref="DemoSeeder.ResetAndFill"/>.
    /// </summary>
    internal void Clear()
    {
        Categories.Clear();
        MetricDefinitions.Clear();
        MonthlySnapshots.Clear();
        Friends.Clear();
        KeyRelationships.Clear();
        AppSettings.Clear();

        _nextCategoryId = 1;
        _nextMetricDefinitionId = 1;
        _nextMonthlySnapshotId = 1;
        _nextMetricSnapshotId = 1;
        _nextFriendId = 1;
        _nextKeyRelationshipId = 1;
        _nextSocialSnapshotId = 1;
    }

    internal Category Add(Category category)
    {
        Categories.Add(category.WithId(_nextCategoryId++));
        return category;
    }

    internal MetricDefinition Add(MetricDefinition metricDefinition)
    {
        MetricDefinitions.Add(metricDefinition.WithId(_nextMetricDefinitionId++));
        return metricDefinition;
    }

    internal Friend Add(Friend friend)
    {
        Friends.Add(friend.WithId(_nextFriendId++));
        return friend;
    }

    internal KeyRelationship Add(KeyRelationship keyRelationship)
    {
        KeyRelationships.Add(keyRelationship.WithId(_nextKeyRelationshipId++));
        return keyRelationship;
    }

    internal MonthlySnapshot Add(MonthlySnapshot snapshot)
    {
        MonthlySnapshots.Add(snapshot.WithId(_nextMonthlySnapshotId++));
        AssignChildIds(snapshot);
        return snapshot;
    }

    /// <summary>
    /// Stands in for EF Core's change tracker assigning keys on save. Called
    /// for every tracked aggregate on each commit, since a
    /// <see cref="MonthlySnapshot"/> can gain children after it was first
    /// added (that is exactly what recording this month's entries does).
    /// </summary>
    internal void AssignPendingIds()
    {
        foreach (var snapshot in MonthlySnapshots)
        {
            AssignChildIds(snapshot);
        }
    }

    private void AssignChildIds(MonthlySnapshot snapshot)
    {
        foreach (var metricSnapshot in snapshot.MetricSnapshots)
        {
            if (metricSnapshot.Id == 0)
            {
                metricSnapshot.WithId(_nextMetricSnapshotId++, snapshot.Id);
            }
        }

        if (snapshot.SocialSnapshot is { Id: 0 } socialSnapshot)
        {
            socialSnapshot.WithId(_nextSocialSnapshotId++);
        }
    }
}

internal sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly DemoStore _store;

    public InMemoryCategoryRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Category>>(_store.Categories.OrderBy(c => c.SortOrder).ToList());

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Categories.FirstOrDefault(c => c.Id == id));

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _store.Add(category);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryMetricDefinitionRepository : IMetricDefinitionRepository
{
    private readonly DemoStore _store;

    public InMemoryMetricDefinitionRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<MetricDefinition>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricDefinition>>(
            _store.MetricDefinitions.OrderBy(m => m.CategoryId).ThenBy(m => m.SortOrder).ToList());

    public Task<MetricDefinition?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.MetricDefinitions.FirstOrDefault(m => m.Id == id));

    public Task AddAsync(MetricDefinition metricDefinition, CancellationToken cancellationToken = default)
    {
        _store.Add(metricDefinition);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryMonthlySnapshotRepository : IMonthlySnapshotRepository
{
    private readonly DemoStore _store;

    public InMemoryMonthlySnapshotRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<MonthlySnapshot>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MonthlySnapshot>>(_store.MonthlySnapshots.OrderBy(s => s.Month).ToList());

    public Task<MonthlySnapshot?> GetByMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.MonthlySnapshots.FirstOrDefault(s => s.Month == new DateOnly(month.Year, month.Month, 1)));

    public Task AddAsync(MonthlySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _store.Add(snapshot);
        return Task.CompletedTask;
    }
}

/// <remarks>
/// Mirrors <c>EfMetricSnapshotRepository</c>'s ordering contract exactly:
/// ordered by the review month the snapshot belongs to, never by RecordedAt.
/// A backfilled entry recorded late must not sort as though it were the most
/// recent reading, or every trend built on it would be wrong.
/// </remarks>
internal sealed class InMemoryMetricSnapshotRepository : IMetricSnapshotRepository
{
    private readonly DemoStore _store;

    public InMemoryMetricSnapshotRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<MetricSnapshot>> GetForMetricAsync(
        int metricDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricSnapshot>>(
            Query(metricDefinitionId).Select(entry => entry.MetricSnapshot).ToList());

    public Task<IReadOnlyList<MetricTrendPoint>> GetTrendForMetricAsync(
        int metricDefinitionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MetricTrendPoint>>(
            Query(metricDefinitionId)
                .Select(entry => new MetricTrendPoint(entry.Month, entry.MetricSnapshot.Value))
                .ToList());

    private IEnumerable<(DateOnly Month, MetricSnapshot MetricSnapshot)> Query(int metricDefinitionId) =>
        _store.MonthlySnapshots
            .SelectMany(snapshot => snapshot.MetricSnapshots, (snapshot, metricSnapshot) => (snapshot.Month, metricSnapshot))
            .Where(entry => entry.metricSnapshot.MetricDefinitionId == metricDefinitionId)
            .OrderBy(entry => entry.Month);
}

internal sealed class InMemoryFriendRepository : IFriendRepository
{
    private readonly DemoStore _store;

    public InMemoryFriendRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<Friend>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Friend>>(_store.Friends.ToList());

    public Task<Friend?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Friends.FirstOrDefault(f => f.Id == id));

    public Task AddAsync(Friend friend, CancellationToken cancellationToken = default)
    {
        _store.Add(friend);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryKeyRelationshipRepository : IKeyRelationshipRepository
{
    private readonly DemoStore _store;

    public InMemoryKeyRelationshipRepository(DemoStore store) => _store = store;

    public Task<IReadOnlyList<KeyRelationship>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<KeyRelationship>>(_store.KeyRelationships.ToList());

    public Task<KeyRelationship?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.KeyRelationships.FirstOrDefault(r => r.Id == id));
}

internal sealed class InMemoryAppSettingRepository : IAppSettingRepository
{
    private readonly DemoStore _store;

    public InMemoryAppSettingRepository(DemoStore store) => _store = store;

    public Task<AppSetting?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.AppSettings.FirstOrDefault(s => s.Key == key));

    public Task<IReadOnlyList<AppSetting>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppSetting>>(_store.AppSettings.ToList());

    public Task AddAsync(AppSetting setting, CancellationToken cancellationToken = default)
    {
        _store.AppSettings.Add(setting);
        return Task.CompletedTask;
    }
}

/// <remarks>
/// There is no transaction to commit against a list, but the identity
/// assignment EF Core performs on save still has to happen, so this is not a
/// no-op: it is the point at which newly added children get their keys.
/// </remarks>
internal sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly DemoStore _store;

    public InMemoryUnitOfWork(DemoStore store) => _store = store;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _store.AssignPendingIds();
        return Task.FromResult(0);
    }
}
