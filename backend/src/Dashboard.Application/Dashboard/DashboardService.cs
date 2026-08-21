using Dashboard.Application.Metrics;
using Dashboard.Application.Settings;
using Dashboard.Application.Social;
using Dashboard.Domain.Metrics;

namespace Dashboard.Application.Dashboard;

/// <summary>
/// Answers "how am I doing overall?" — the one question this whole app
/// exists to answer. Turns every active metric's status into a category
/// score, blends categories into an overall score, and collects anything
/// that needs attention.
/// </summary>
public sealed class DashboardService
{
    /// <summary>
    /// Social isn't backed by a real Category row (it's Friend/SocialSnapshot
    /// data, not a MetricDefinition category), so it gets a synthetic
    /// CategorySummary with this sentinel id -- guaranteed not to collide
    /// with a real category's autoincremented id, which starts at 1.
    /// </summary>
    private const int SocialCategoryId = -1;

    /// <summary>
    /// Social's two facets (Active Circle size, Circle Upkeep maintenance)
    /// are surfaced as their own alerts rather than under a single "Social"
    /// metric id, so these sentinels exist for the same reason
    /// <see cref="SocialCategoryId"/> does -- there's no real MetricDefinition
    /// row backing them.
    /// </summary>
    private const int SocialActiveCircleAlertId = -2;
    private const int SocialUpkeepAlertId = -3;

    private readonly ICategoryRepository _categoryRepository;
    private readonly IMetricDefinitionRepository _metricDefinitionRepository;
    private readonly IMetricSnapshotRepository _metricSnapshotRepository;
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly MetricEvaluationService _metricEvaluationService;
    private readonly SocialService _socialService;

    public DashboardService(
        ICategoryRepository categoryRepository,
        IMetricDefinitionRepository metricDefinitionRepository,
        IMetricSnapshotRepository metricSnapshotRepository,
        IAppSettingRepository appSettingRepository,
        MetricEvaluationService metricEvaluationService,
        SocialService socialService)
    {
        _categoryRepository = categoryRepository;
        _metricDefinitionRepository = metricDefinitionRepository;
        _metricSnapshotRepository = metricSnapshotRepository;
        _appSettingRepository = appSettingRepository;
        _metricEvaluationService = metricEvaluationService;
        _socialService = socialService;
    }

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categoryRepository.GetAllAsync(cancellationToken);
        var allMetrics = await _metricDefinitionRepository.GetAllAsync(cancellationToken);
        var statusThresholds = await CategoryStatusCalculator.GetThresholdsAsync(_appSettingRepository, cancellationToken);

        var categorySummaries = new List<CategorySummary>();
        var alerts = new List<DashboardAlert>();

        foreach (var category in categories)
        {
            var metricsInCategory = allMetrics
                .Where(metric => metric.CategoryId == category.Id && metric.IsActive)
                .ToList();

            var metricSummaries = new List<MetricSummary>();
            var scores = new List<int>();

            foreach (var metric in metricsInCategory)
            {
                var status = await _metricEvaluationService.EvaluateAsync(metric.Id, cancellationToken);

                // No trend yet (fewer than 2 months recorded) falls back to a
                // level-based score off the metric's configured rating (e.g.
                // Net Worth, Credit Score), so the very first month isn't
                // stuck showing "no data" everywhere. Metrics with no rating
                // and no trend genuinely have nothing to score yet, so
                // they're excluded (see MetricScoring.GetScoreAsync).
                var score = await MetricScoring.GetScoreAsync(
                    status, metric.Id, metric.Name, _metricSnapshotRepository, _appSettingRepository, cancellationToken);
                if (score is not null)
                {
                    scores.Add(score.Value);
                }

                metricSummaries.Add(new MetricSummary(metric.Id, metric.Name, status, score));

                // Trend-based wording ("has declined"/"has stalled") is the
                // more informative message when there's enough history to
                // compute one. But a metric can also be dragging its
                // category down on level alone with no trend yet at all --
                // e.g. month one, where every trend is InsufficientData but
                // Net Worth's score is still only 48/100 -- so a score-based
                // alert fills that gap instead of "Needs Attention" staying
                // silent while the category badge says otherwise.
                var metricScoreStatus = CategoryStatusCalculator.From(score, statusThresholds);
                if (status is MetricStatus.Regressed or MetricStatus.Stagnant)
                {
                    alerts.Add(new DashboardAlert(
                        metric.Id, metric.Name, category.Name, BuildAlertMessage(metric.Name, status), score, metricScoreStatus));
                }
                else if (metricScoreStatus is CategoryStatus.Struggling or CategoryStatus.NeedsAttention)
                {
                    alerts.Add(new DashboardAlert(
                        metric.Id, metric.Name, category.Name, BuildScoreAlertMessage(metric.Name, metricScoreStatus),
                        score, metricScoreStatus));
                }
            }

            var categoryScore = scores.Count == 0 ? (int?)null : (int)Math.Round(scores.Average());

            categorySummaries.Add(new CategorySummary(
                category.Id, category.Name, CategoryStatusCalculator.From(categoryScore, statusThresholds), categoryScore, metricSummaries));
        }

        // Social's Score/Status are computed by SocialService itself (not
        // recomputed here, since they're also shown directly on the Social
        // page's own header) -- one source of truth rather than two places
        // that could quietly drift apart.
        var socialSummary = await _socialService.GetSummaryAsync(cancellationToken);
        categorySummaries.Add(new CategorySummary(
            SocialCategoryId, "Social", socialSummary.Status, socialSummary.Score, Array.Empty<MetricSummary>()));

        // Social's blended score can hide which facet is actually the
        // problem -- a thin-but-well-kept circle and a large-but-neglected
        // one can land on the same blended number -- so alerts are raised
        // per facet, naming whichever one (Active Circle, Circle Upkeep, or
        // a specific key relationship) is actually dragging, the same way a
        // low Net Worth (not Credit Score) gets called out by name under
        // Finance above.
        var activeCircleStatus = CategoryStatusCalculator.From(socialSummary.RatingScore, statusThresholds);
        if (activeCircleStatus is CategoryStatus.Struggling or CategoryStatus.NeedsAttention)
        {
            alerts.Add(new DashboardAlert(
                SocialActiveCircleAlertId, "Active Circle", "Social",
                BuildScoreAlertMessage("Active Circle", activeCircleStatus),
                socialSummary.RatingScore, activeCircleStatus));
        }

        if (socialSummary.MaintenanceScore is { } maintenanceScoreValue)
        {
            var upkeepStatus = CategoryStatusCalculator.From(maintenanceScoreValue, statusThresholds);
            if (upkeepStatus is CategoryStatus.Struggling or CategoryStatus.NeedsAttention)
            {
                alerts.Add(new DashboardAlert(
                    SocialUpkeepAlertId, "Circle Upkeep", "Social",
                    BuildScoreAlertMessage("Circle Upkeep", upkeepStatus),
                    maintenanceScoreValue, upkeepStatus));
            }
        }

        foreach (var keyRelationship in socialSummary.KeyRelationships)
        {
            var keyRelationshipStatus = CategoryStatusCalculator.From(keyRelationship.Score, statusThresholds);
            if (keyRelationshipStatus is CategoryStatus.Struggling or CategoryStatus.NeedsAttention)
            {
                alerts.Add(new DashboardAlert(
                    keyRelationship.KeyRelationshipId, keyRelationship.Label, "Social",
                    BuildScoreAlertMessage(keyRelationship.Label, keyRelationshipStatus),
                    keyRelationship.Score, keyRelationshipStatus));
            }
        }

        var scoredCategoryScores = categorySummaries
            .Where(summary => summary.Score.HasValue)
            .Select(summary => summary.Score!.Value)
            .ToList();

        var overallScore = scoredCategoryScores.Count == 0
            ? (int?)null
            : (int)Math.Round(scoredCategoryScores.Average());
        var overallStatus = CategoryStatusCalculator.From(overallScore, statusThresholds);

        return new DashboardSummary(overallScore, overallStatus, categorySummaries, alerts);
    }

    private static string BuildAlertMessage(string metricName, MetricStatus status) => status switch
    {
        MetricStatus.Stagnant => $"{metricName} has stalled",
        MetricStatus.Regressed => $"{metricName} has declined",
        _ => $"{metricName} needs attention",
    };

    /// <summary>
    /// Used when there's no trend to describe yet (or none of interest) but
    /// the metric's absolute score is still low enough to be the reason its
    /// category reads "Needs attention". The score itself travels alongside
    /// on <see cref="DashboardAlert.Score"/> rather than being baked into
    /// the text, since the frontend renders it as its own score bar.
    /// </summary>
    private static string BuildScoreAlertMessage(string name, CategoryStatus status) => status switch
    {
        CategoryStatus.Struggling => $"{name} needs attention",
        CategoryStatus.NeedsAttention => $"{name} is trailing",
        _ => $"{name} needs attention",
    };
}
