using Dashboard.Application.Metrics;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Tests.Metrics;

public class MetricEntryServiceTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeMetricDefinitionRepository _metricDefinitions = new();
    private readonly FakeMonthlySnapshotRepository _monthlySnapshots = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private MetricEntryService CreateService() =>
        new(_categories, _metricDefinitions, _monthlySnapshots, _unitOfWork);

    [Fact]
    public async Task RecordEntriesAsync_WithUnknownCategory_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordEntriesAsync(999, new DateOnly(2026, 7, 1), new Dictionary<int, decimal>()));
    }

    [Fact]
    public async Task RecordEntriesAsync_WithAMetricFromAnotherCategory_Throws()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _categories.Seed(2, new Category("Finance", sortOrder: 1));
        _metricDefinitions.Seed(1, new MetricDefinition(
            2, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.RecordEntriesAsync(1, new DateOnly(2026, 7, 1), new Dictionary<int, decimal> { [1] = 1050 }));
    }

    [Fact]
    public async Task RecordEntriesAsync_WhenTheMonthHasNoSnapshotYet_CreatesOneAndRecordsTheValues()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var service = CreateService();

        await service.RecordEntriesAsync(1, new DateOnly(2026, 7, 15), new Dictionary<int, decimal> { [1] = 1050 });

        var snapshot = await _monthlySnapshots.GetByMonthAsync(new DateOnly(2026, 7, 1));
        Assert.NotNull(snapshot);
        var recorded = Assert.Single(snapshot!.MetricSnapshots);
        Assert.Equal(1050, recorded.Value);
        Assert.Equal(1, _unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task RecordEntriesAsync_WhenTheMonthAlreadyHasASnapshot_UpsertsRatherThanDuplicating()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var existingSnapshot = new MonthlySnapshot(new DateOnly(2026, 7, 1), DateTimeOffset.UtcNow);
        existingSnapshot.AddMetricSnapshot(1, 1000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(existingSnapshot);

        var service = CreateService();

        await service.RecordEntriesAsync(1, new DateOnly(2026, 7, 1), new Dictionary<int, decimal> { [1] = 1050 });

        var snapshot = await _monthlySnapshots.GetByMonthAsync(new DateOnly(2026, 7, 1));
        var recorded = Assert.Single(snapshot!.MetricSnapshots);
        Assert.Equal(1050, recorded.Value);
    }

    private void SeedStrengthLifts()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(1, "Squat", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
        _metricDefinitions.Seed(2, new MetricDefinition(1, "Bench Press", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 1));
        _metricDefinitions.Seed(3, new MetricDefinition(1, "Deadlift", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 2));
        _metricDefinitions.Seed(4, new MetricDefinition(1, "Overhead Press", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 3));
        _metricDefinitions.Seed(5, new MetricDefinition(
            1, "Strength Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 4, isCalculated: true));
    }

    [Fact]
    public async Task RecordEntriesAsync_WithAllFourLiftsSubmittedTogether_AutoCalculatesTheTotal()
    {
        SeedStrengthLifts();
        var service = CreateService();

        await service.RecordEntriesAsync(1, new DateOnly(2026, 7, 1), new Dictionary<int, decimal>
        {
            [1] = 315, // Squat
            [2] = 225, // Bench Press
            [3] = 405, // Deadlift
            [4] = 135, // Overhead Press
        });

        var snapshot = await _monthlySnapshots.GetByMonthAsync(new DateOnly(2026, 7, 1));
        var total = snapshot!.MetricSnapshots.Single(s => s.MetricDefinitionId == 5);
        Assert.Equal(1080, total.Value);
    }

    [Fact]
    public async Task RecordEntriesAsync_WithOnlySomeLiftsEverRecorded_DoesNotCalculateATotalYet()
    {
        SeedStrengthLifts();
        var service = CreateService();

        await service.RecordEntriesAsync(1, new DateOnly(2026, 7, 1), new Dictionary<int, decimal> { [1] = 315 });

        var snapshot = await _monthlySnapshots.GetByMonthAsync(new DateOnly(2026, 7, 1));
        Assert.DoesNotContain(snapshot!.MetricSnapshots, s => s.MetricDefinitionId == 5);
    }

    [Fact]
    public async Task RecordEntriesAsync_UsesEachLiftsLatestValueEvenIfNotResubmittedThisMonth()
    {
        SeedStrengthLifts();
        var service = CreateService();

        // Month 1: all four lifts entered.
        await service.RecordEntriesAsync(1, new DateOnly(2026, 6, 1), new Dictionary<int, decimal>
        {
            [1] = 300,
            [2] = 200,
            [3] = 400,
            [4] = 130,
        });

        // Month 2: only Squat improves; the others aren't re-entered.
        await service.RecordEntriesAsync(1, new DateOnly(2026, 7, 1), new Dictionary<int, decimal> { [1] = 315 });

        var julySnapshot = await _monthlySnapshots.GetByMonthAsync(new DateOnly(2026, 7, 1));
        var total = julySnapshot!.MetricSnapshots.Single(s => s.MetricDefinitionId == 5);
        Assert.Equal(315 + 200 + 400 + 130, total.Value);
    }
}
