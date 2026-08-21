using Vantage.Domain.Metrics;

namespace Vantage.Application.Metrics;

public interface IMonthlySnapshotRepository
{
    Task<IReadOnlyList<MonthlySnapshot>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<MonthlySnapshot?> GetByMonthAsync(DateOnly month, CancellationToken cancellationToken = default);

    Task AddAsync(MonthlySnapshot snapshot, CancellationToken cancellationToken = default);
}
