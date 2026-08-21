using Vantage.Application.Dashboard;
using Vantage.Application.Settings;
using Vantage.Domain.Metrics;

namespace Vantage.Application.Metrics;

/// <summary>
/// Answers "how is this one category doing, metric by metric?" — the data
/// behind a category's detail screen (e.g. Fitness). Unlike
/// <see cref="Dashboard.DashboardService"/>, this also surfaces each
/// metric's current-month value specifically, since that's what an entry
/// form needs to pre-fill correctly (the latest recorded value might be last
/// month's, if this month hasn't been entered yet).
/// </summary>
public sealed class CategoryDetailService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricSnapshotRepository _metricSnapshotRepository;
    private readonly IMonthlySnapshotRepository _monthlySnapshotRepository;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly MetricEvaluationService _metricEvaluationService;

    public CategoryDetailService(
        ICategoryRepository categoryRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricSnapshotRepository metricSnapshotRepository,
        IMonthlySnapshotRepository monthlySnapshotRepository,
        IAppSettingRepository appSettingRepository,
        MetricEvaluationService metricEvaluationService)
    {
        _categoryRepository = categoryRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _metricSnapshotRepository = metricSnapshotRepository;
        _monthlySnapshotRepository = monthlySnapshotRepository;
        _appSettingRepository = appSettingRepository;
        _metricEvaluationService = metricEvaluationService;
    }

    public async Task<CategoryDetail> GetDetailAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new InvalidOperationException($"Category {categoryId} was not found.");

        var metrics = (await _metricDefinitionRepository.GetAllAsync(cancellationToken))
            .Where(metric => metric.CategoryId == categoryId && metric.IsActive)
            .OrderBy(metric => metric.SortOrder)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);
        var currentMonthSnapshot = await _monthlySnapshotRepository.GetByMonthAsync(currentMonth, cancellationToken);
        var currentMonthValuesByMetricId = currentMonthSnapshot?.MetricSnapshots
            .ToDictionary(snapshot => snapshot.MetricDefinitionId, snapshot => snapshot.Value)
            ?? new Dictionary<int, decimal>();

        var metricDetails = new List<MetricDetail>();
        var scores = new List<int>();
        foreach (var metric in metrics)
        {
            var status = await _metricEvaluationService.EvaluateAsync(metric.Id, cancellationToken);
            var snapshots = await _metricSnapshotRepository.GetForMetricAsync(metric.Id, cancellationToken);
            var latestValue = snapshots.Count == 0 ? (decimal?)null : snapshots[^1].Value;
            var currentMonthValue = currentMonthValuesByMetricId.TryGetValue(metric.Id, out var value) ? value : (decimal?)null;

            string? ratingLabel = null;
            MetricRatingTier? ratingTier = null;
            string? ratingDescription = null;
            IReadOnlyList<RatingBand>? ratingBands = null;
            if (latestValue is not null)
            {
                (ratingLabel, ratingTier, ratingDescription, ratingBands) =
                    await GetRatingAsync(metric.Name, latestValue.Value, cancellationToken);
            }

            // Same scoring MetricScoring.GetScoreAsync gives DashboardService
            // for this metric, so both the metric's own score and the
            // category average built from it always match what the
            // Dashboard shows for this category.
            var score = await MetricScoring.GetScoreAsync(
                status, metric.Id, metric.Name, _metricSnapshotRepository, _appSettingRepository, cancellationToken);
            if (score is not null)
            {
                scores.Add(score.Value);
            }

            metricDetails.Add(new MetricDetail(
                metric.Id, metric.Name, metric.Unit, latestValue, currentMonthValue, status,
                ratingLabel, ratingTier, ratingDescription, score, metric.IsCalculated, ratingBands));
        }

        var categoryScore = scores.Count == 0 ? (int?)null : (int)Math.Round(scores.Average());
        var statusThresholds = await CategoryStatusCalculator.GetThresholdsAsync(_appSettingRepository, cancellationToken);
        var categoryStatus = CategoryStatusCalculator.From(categoryScore, statusThresholds);

        return new CategoryDetail(category.Id, category.Name, categoryScore, categoryStatus, metricDetails);
    }

    private async Task<(string? Label, MetricRatingTier? Tier, string? Description, IReadOnlyList<RatingBand>? Bands)> GetRatingAsync(
        string metricName, decimal latestValue, CancellationToken cancellationToken)
    {
        var ratingDefinition = KnownMetricRatings.ForMetricName(metricName);
        if (ratingDefinition is null)
        {
            return (null, null, null, null);
        }

        var thresholds = await ratingDefinition.GetThresholdsAsync(_appSettingRepository, cancellationToken);
        var tier = MetricRatingCalculator.Rate(latestValue, thresholds);
        var description = await ratingDefinition.GetDescriptionAsync(tier, _appSettingRepository, cancellationToken);
        var bands = ratingDefinition.DescribeBands(thresholds);
        return (ratingDefinition.LabelFor(tier), tier, description, bands);
    }
}
