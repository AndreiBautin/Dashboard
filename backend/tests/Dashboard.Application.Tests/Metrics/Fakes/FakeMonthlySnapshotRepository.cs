using Dashboard.Application.Metrics;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Tests.Metrics.Fakes;

public sealed class FakeMonthlySnapshotRepository : IMonthlySnapshotRepository
{
    private readonly List<MonthlySnapshot> _snapshots = [];

    public void Seed(MonthlySnapshot snapshot) => _snapshots.Add(snapshot);

    public Task<IReadOnlyList<MonthlySnapshot>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MonthlySnapshot>>(_snapshots.OrderBy(s => s.Month).ToList());

    public Task<MonthlySnapshot?> GetByMonthAsync(DateOnly month, CancellationToken cancellationToken = default) =>
        Task.FromResult(_snapshots.FirstOrDefault(s => s.Month == month));

    public Task AddAsync(MonthlySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _snapshots.Add(snapshot);
        return Task.CompletedTask;
    }
}
