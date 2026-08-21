using Vantage.Domain.Metrics;

namespace Vantage.Application.Metrics;

/// <summary>
/// Records a monthly review's worth of metric values for one category —
/// the write side behind an entry screen (e.g. "enter this month's Fitness
/// numbers"). Gets or creates that month's <see cref="MonthlySnapshot"/> and
/// upserts each value onto it, so submitting the same month twice (fixing a
/// typo, say) updates in place rather than erroring or duplicating.
/// </summary>
public sealed class MetricEntryService
{
    // Strength Total is the one calculated metric this app knows how to
    // derive today. Matched by name (see KnownMetricRatings for the same
    // pattern) rather than a generic "formula" concept -- not worth
    // building a real expression engine for a single derived metric.
    private static readonly string[] StrengthLiftNames = ["Squat", "Bench Press", "Deadlift", "Overhead Press"];
    private const string StrengthTotalMetricName = "Strength Total";

    private readonly ICategoryRepository _categoryRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMonthlySnapshotRepository _monthlySnapshotRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MetricEntryService(
        ICategoryRepository categoryRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        IMonthlySnapshotRepository monthlySnapshotRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _monthlySnapshotRepository = monthlySnapshotRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RecordEntriesAsync(
        int categoryId,
        DateOnly month,
        IReadOnlyDictionary<int, decimal> valuesByMetricDefinitionId,
        CancellationToken cancellationToken = default)
    {
        _ = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Category {categoryId} was not found.");

        var metricsInCategory = (await _metricDefinitionRepository.GetAllAsync(cancellationToken))
            .Where(metric => metric.CategoryId == categoryId && metric.IsActive)
            .ToList();
        var metricIdsInCategory = metricsInCategory.Select(m => m.Id).ToHashSet();

        foreach (var metricDefinitionId in valuesByMetricDefinitionId.Keys)
        {
            if (!metricIdsInCategory.Contains(metricDefinitionId))
            {
                throw new InvalidOperationException(
                    $"Metric {metricDefinitionId} does not belong to category {categoryId}.");
            }
        }

        var normalizedMonth = new DateOnly(month.Year, month.Month, 1);
        var monthlySnapshot = await _monthlySnapshotRepository.GetByMonthAsync(normalizedMonth, cancellationToken);
        if (monthlySnapshot is null)
        {
            monthlySnapshot = new MonthlySnapshot(normalizedMonth, DateTimeOffset.UtcNow);
            await _monthlySnapshotRepository.AddAsync(monthlySnapshot, cancellationToken);
        }

        var recordedAt = DateTimeOffset.UtcNow;
        foreach (var (metricDefinitionId, value) in valuesByMetricDefinitionId)
        {
            monthlySnapshot.SetMetricValue(metricDefinitionId, value, recordedAt);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await RecomputeCalculatedMetricsAsync(metricsInCategory, monthlySnapshot, recordedAt, cancellationToken);
    }

    /// <summary>
    /// Recomputes any calculated metric (currently just Strength Total) using
    /// each dependency's most recently recorded value across every month, not
    /// just this one -- so a lift you haven't re-tested this month still
    /// contributes its last known max rather than dropping out of the total.
    /// </summary>
    private async Task RecomputeCalculatedMetricsAsync(
        IReadOnlyList<MetricDefinition> metricsInCategory,
        MonthlySnapshot monthlySnapshot,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        var strengthTotal = metricsInCategory.FirstOrDefault(
            metric => metric.IsCalculated && metric.Name == StrengthTotalMetricName);
        if (strengthTotal is null)
        {
            return;
        }

        var lifts = new List<MetricDefinition>();
        foreach (var liftName in StrengthLiftNames)
        {
            var lift = metricsInCategory.FirstOrDefault(metric => metric.Name == liftName);
            if (lift is null)
            {
                return; // This category isn't set up with the expected lifts -- leave the total alone.
            }

            lifts.Add(lift);
        }

        var allMonths = await _monthlySnapshotRepository.GetAllAsync(cancellationToken);

        decimal total = 0;
        foreach (var lift in lifts)
        {
            var matchingSnapshots = allMonths
                .SelectMany(snapshot => snapshot.MetricSnapshots, (snapshot, metricSnapshot) => (snapshot.Month, metricSnapshot))
                .Where(entry => entry.metricSnapshot.MetricDefinitionId == lift.Id)
                .OrderByDescending(entry => entry.Month)
                .ToList();

            if (matchingSnapshots.Count == 0)
            {
                return; // Can't compute a meaningful total until every lift has at least one recorded value.
            }

            total += matchingSnapshots[0].metricSnapshot.Value;
        }

        monthlySnapshot.SetMetricValue(strengthTotal.Id, total, recordedAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
