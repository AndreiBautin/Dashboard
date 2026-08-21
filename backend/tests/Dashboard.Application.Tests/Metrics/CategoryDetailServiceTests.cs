using Dashboard.Application.Dashboard;
using Dashboard.Application.Metrics;
using Dashboard.Application.Tests.Metrics.Fakes;
using Dashboard.Application.Tests.Social.Fakes;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Metrics.Evaluators;
using Dashboard.Domain.Settings;

namespace Dashboard.Application.Tests.Metrics;

public class CategoryDetailServiceTests
{
    private readonly FakeCategoryRepository _categories = new();
    private readonly FakeMetricDefinitionRepository _metricDefinitions = new();
    private readonly FakeMetricSnapshotRepository _metricSnapshots = new();
    private readonly FakeMonthlySnapshotRepository _monthlySnapshots = new();
    private readonly FakeAppSettingRepository _appSettings = new();

    private CategoryDetailService CreateService()
    {
        var evaluatorFactory = new MetricEvaluatorFactory(
        [
            new IncreaseMetricEvaluator(),
            new DecreaseMetricEvaluator(),
            new StayAboveMetricEvaluator(),
            new StayBelowMetricEvaluator(),
            new StayWithinRangeMetricEvaluator(),
        ]);
        var evaluationService = new MetricEvaluationService(_metricDefinitions, _metricSnapshots, evaluatorFactory);

        return new CategoryDetailService(
            _categories, _metricDefinitions, _metricSnapshots, _monthlySnapshots, _appSettings, evaluationService);
    }

    private static DateOnly ThisMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new DateOnly(today.Year, today.Month, 1);
    }

    private static DateOnly LastMonth() => ThisMonth().AddMonths(-1);

    [Fact]
    public async Task GetDetailAsync_WithUnknownCategory_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDetailAsync(999));
    }

    [Fact]
    public async Task GetDetailAsync_WhenThisMonthIsAlreadyRecorded_LatestAndCurrentMonthValuesMatch()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var lastMonthSnapshot = new MonthlySnapshot(LastMonth(), DateTimeOffset.UtcNow.AddMonths(-1));
        var lastMonthMetricSnapshot = lastMonthSnapshot.AddMetricSnapshot(1, 1000, DateTimeOffset.UtcNow.AddMonths(-1));
        _monthlySnapshots.Seed(lastMonthSnapshot);

        var thisMonthSnapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var thisMonthMetricSnapshot = thisMonthSnapshot.AddMetricSnapshot(1, 1050, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(thisMonthSnapshot);

        _metricSnapshots.SeedSnapshots(1, lastMonthMetricSnapshot, thisMonthMetricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Equal("Fitness", detail.CategoryName);
        var metric = Assert.Single(detail.Metrics);
        Assert.Equal("Powerlifting Total", metric.MetricName);
        Assert.Equal(1050, metric.LatestValue);
        Assert.Equal(1050, metric.CurrentMonthValue);
    }

    [Fact]
    public async Task GetDetailAsync_WhenThisMonthIsNotYetRecorded_CurrentMonthValueIsNullButLatestValueIsLastMonths()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var lastMonthSnapshot = new MonthlySnapshot(LastMonth(), DateTimeOffset.UtcNow.AddMonths(-1));
        var lastMonthMetricSnapshot = lastMonthSnapshot.AddMetricSnapshot(1, 1000, DateTimeOffset.UtcNow.AddMonths(-1));
        _monthlySnapshots.Seed(lastMonthSnapshot);
        _metricSnapshots.SeedSnapshots(1, lastMonthMetricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        var metric = Assert.Single(detail.Metrics);
        Assert.Equal(1000, metric.LatestValue);
        Assert.Null(metric.CurrentMonthValue);
    }

    [Fact]
    public async Task GetDetailAsync_ExcludesInactiveMetrics()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        var inactiveMetric = new MetricDefinition(
            1, "Retired Metric", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0);
        typeof(MetricDefinition).GetProperty(nameof(MetricDefinition.IsActive))!.SetValue(inactiveMetric, false);
        _metricDefinitions.Seed(1, inactiveMetric);

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Empty(detail.Metrics);
    }

    [Fact]
    public async Task GetDetailAsync_WithNoValueYet_RatingLabelIsNull()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var detail = await CreateService().GetDetailAsync(1);

        var metric = Assert.Single(detail.Metrics);
        Assert.Null(metric.RatingLabel);
        Assert.Null(metric.RatingTier);
        Assert.Null(metric.RatingDescription);
    }

    [Fact]
    public async Task GetDetailAsync_WithAValueAndTheDefaultThresholds_ComputesTheRatingLabel()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        // 170,000 is below the default NetWorthTier2Max (250,000) and above
        // Tier1Max (50,000), so "Growing" (Tier2).
        var metric = Assert.Single(detail.Metrics);
        Assert.Equal("Growing", metric.RatingLabel);
        Assert.Equal(MetricRatingTier.Tier2, metric.RatingTier);
        Assert.Equal("Steady progress with real momentum building.", metric.RatingDescription);
    }

    [Fact]
    public async Task GetDetailAsync_EmergencyFundPastItsTopCutoff_ScoresOneHundred()
    {
        // Past the top-defined cutoff, RateContinuous is a flat 100 for
        // every rated metric now (see MetricRatingCalculatorTests) -- once
        // you've cleared every threshold there's nothing further to measure
        // progress against, so $34,500 (past the default 30,000 "Almost
        // There" cutoff) scores 100, matching its "Well Funded" tier badge.
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Emergency Fund", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 34_500, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        var metric = Assert.Single(detail.Metrics);
        Assert.Equal("Well Funded", metric.RatingLabel);
        Assert.Equal(100, metric.Score);
    }

    [Fact]
    public async Task GetDetailAsync_ExposesTheFullRatingScale_AscendingByRawValue()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        // Default Net Worth thresholds: 50,000 / 250,000 / 750,000.
        var bands = Assert.Single(detail.Metrics).RatingBands;
        Assert.NotNull(bands);
        Assert.Equal(
            [
                new RatingBand(50_000, "Building"),
                new RatingBand(250_000, "Growing"),
                new RatingBand(750_000, "Strong"),
                new RatingBand(null, "Thriving"),
            ],
            bands);
    }

    [Fact]
    public async Task GetDetailAsync_ExposesTheFullRatingScale_InCorrectOrderWhenLowerIsBetter()
    {
        // Waist Measurement is HigherIsBetter: false -- the smallest values
        // get the *best* label (Lean), not the worst one, so the band
        // breakdown (still ascending by raw inches) needs its labels
        // correctly inverted rather than naively paired positionally with
        // Tier1Max/Tier2Max/Tier3Max.
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Waist Measurement", "in", EvaluationStrategy.Decrease, new EvaluationConfig(), sortOrder: 0));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 33m, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        // Default Waist Measurement thresholds: 32 / 36 / 40 (in).
        var bands = Assert.Single(detail.Metrics).RatingBands;
        Assert.NotNull(bands);
        Assert.Equal(
            [
                new RatingBand(32, "Lean"),
                new RatingBand(36, "Trim"),
                new RatingBand(40, "Elevated"),
                new RatingBand(null, "High"),
            ],
            bands);
    }

    [Fact]
    public async Task GetDetailAsync_UsesConfiguredRatingThresholds_NotHardcodedDefaults()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
        _appSettings.Seed(new AppSetting("NetWorthTier1Max", "500000"));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        // Would default to "Growing", but a configured Tier1Max of 500,000
        // pushes 170,000 into "Building" instead -- proves the setting is read.
        Assert.Equal("Building", Assert.Single(detail.Metrics).RatingLabel);
    }

    [Fact]
    public async Task GetDetailAsync_UsesConfiguredRatingDescription_NotHardcodedDefault()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
        _appSettings.Seed(new AppSetting("NetWorthTier2Description", "Custom growing blurb."));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Equal("Custom growing blurb.", Assert.Single(detail.Metrics).RatingDescription);
    }

    [Fact]
    public async Task GetDetailAsync_SurfacesIsCalculatedFromTheMetricDefinition()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Strength Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0, isCalculated: true));

        var detail = await CreateService().GetDetailAsync(1);

        Assert.True(Assert.Single(detail.Metrics).IsCalculated);
    }

    [Fact]
    public async Task GetDetailAsync_WithATwoMonthTrend_SurfacesTheSameScoreTheDashboardWould()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var lastMonthSnapshot = new MonthlySnapshot(LastMonth(), DateTimeOffset.UtcNow.AddMonths(-1));
        var lastMonthMetricSnapshot = lastMonthSnapshot.AddMetricSnapshot(1, 1000, DateTimeOffset.UtcNow.AddMonths(-1));
        _monthlySnapshots.Seed(lastMonthSnapshot);

        var thisMonthSnapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var thisMonthMetricSnapshot = thisMonthSnapshot.AddMetricSnapshot(1, 1050, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(thisMonthSnapshot);

        _metricSnapshots.SeedSnapshots(1, lastMonthMetricSnapshot, thisMonthMetricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Equal(100, detail.Score); // improved -> 100, same scale DashboardService uses
        Assert.Equal(100, Assert.Single(detail.Metrics).Score);
    }

    [Fact]
    public async Task GetDetailAsync_WithOnlyOneMonthRecorded_RatedMetricsStillContributeAScore()
    {
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var metricSnapshot = snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, metricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        // Same continuous rated-value fallback DashboardService uses -- see
        // its "RatedMetricsStillContributeAScore" test for the 40 math.
        Assert.Equal(40, detail.Score);
        Assert.Equal(40, Assert.Single(detail.Metrics).Score);
    }

    [Fact]
    public async Task GetDetailAsync_WithNoScoreableMetrics_ScoreIsNull()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
        // No snapshots at all -- InsufficientData and no rating definition either.

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Null(detail.Score);
        Assert.Null(Assert.Single(detail.Metrics).Score);
    }

    [Fact]
    public async Task GetDetailAsync_CategoryScoreIsTheAverageOfEachMetricsOwnScore()
    {
        // Net Worth $170,000 sits exactly 60% through the default Tier2 band
        // (50,000-250,000), which maps onto the 25-50 score slice ->
        // 25 + 0.6 * 25 = 40. Credit Score 760 is past its top cutoff of 739,
        // so a flat continuous score of 100. The category score should be the
        // average of the two metrics' own scores, (40 + 100) / 2 = 70.
        _categories.Seed(1, new Category("Finance", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Net Worth", "USD", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));
        _metricDefinitions.Seed(2, new MetricDefinition(
            1, "Credit Score", "points", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 1));

        var snapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        snapshot.AddMetricSnapshot(1, 170_000, DateTimeOffset.UtcNow);
        snapshot.AddMetricSnapshot(2, 760, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(snapshot);
        _metricSnapshots.SeedSnapshots(1, new MetricSnapshot(1, 170_000, DateTimeOffset.UtcNow));
        _metricSnapshots.SeedSnapshots(2, new MetricSnapshot(2, 760, DateTimeOffset.UtcNow));

        var detail = await CreateService().GetDetailAsync(1);

        var netWorth = detail.Metrics.Single(m => m.MetricName == "Net Worth");
        var creditScore = detail.Metrics.Single(m => m.MetricName == "Credit Score");
        Assert.Equal(40, netWorth.Score);
        Assert.Equal(100, creditScore.Score);
        Assert.Equal(70, detail.Score);
        // Against the configured cutoffs (25 / 50 / 75), a 70 is On Track.
        Assert.Equal(CategoryStatus.OnTrack, detail.Status);
    }

    [Fact]
    public async Task GetDetailAsync_StatusMatchesTheSameThresholdsTheDashboardUses()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var lastMonthSnapshot = new MonthlySnapshot(LastMonth(), DateTimeOffset.UtcNow.AddMonths(-1));
        var lastMonthMetricSnapshot = lastMonthSnapshot.AddMetricSnapshot(1, 1000, DateTimeOffset.UtcNow.AddMonths(-1));
        _monthlySnapshots.Seed(lastMonthSnapshot);

        var thisMonthSnapshot = new MonthlySnapshot(ThisMonth(), DateTimeOffset.UtcNow);
        var thisMonthMetricSnapshot = thisMonthSnapshot.AddMetricSnapshot(1, 1050, DateTimeOffset.UtcNow);
        _monthlySnapshots.Seed(thisMonthSnapshot);

        _metricSnapshots.SeedSnapshots(1, lastMonthMetricSnapshot, thisMonthMetricSnapshot);

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Equal(100, detail.Score);
        Assert.Equal(CategoryStatus.Excelling, detail.Status);
    }

    [Fact]
    public async Task GetDetailAsync_WithNoScoreableMetrics_StatusIsNoData()
    {
        _categories.Seed(1, new Category("Fitness", sortOrder: 0));
        _metricDefinitions.Seed(1, new MetricDefinition(
            1, "Powerlifting Total", "lb", EvaluationStrategy.Increase, new EvaluationConfig(), sortOrder: 0));

        var detail = await CreateService().GetDetailAsync(1);

        Assert.Null(detail.Score);
        Assert.Equal(CategoryStatus.NoData, detail.Status);
    }
}
