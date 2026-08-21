namespace Vantage.Domain.Metrics;

/// <summary>
/// A trackable metric within a category (e.g. "Powerlifting Total", kg,
/// Increase). This is the piece of data that makes the app extensible:
/// adding a metric — even one that needs an evaluation rule already covered
/// by an existing strategy — never requires a code change.
/// </summary>
public sealed class MetricDefinition
{
    public int Id { get; private set; }
    public int CategoryId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public EvaluationStrategy EvaluationStrategy { get; private set; }
    public EvaluationConfig EvaluationConfig { get; private set; } = null!;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// True for metrics whose value is derived from other metrics (e.g.
    /// Strength Total = Squat + Bench Press + Deadlift + Overhead Press)
    /// rather than entered directly. The entry form skips these; something
    /// else (see MetricEntryService) recomputes and stores their value
    /// whenever the metrics they depend on change.
    /// </summary>
    public bool IsCalculated { get; private set; }

    // For EF Core materialization only.
    private MetricDefinition()
    {
    }

    public MetricDefinition(
        int categoryId,
        string name,
        string unit,
        EvaluationStrategy evaluationStrategy,
        EvaluationConfig evaluationConfig,
        int sortOrder,
        bool isCalculated = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Metric name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Metric unit is required.", nameof(unit));
        }

        ValidateConfig(evaluationStrategy, evaluationConfig);

        CategoryId = categoryId;
        Name = name;
        Unit = unit;
        EvaluationStrategy = evaluationStrategy;
        EvaluationConfig = evaluationConfig;
        SortOrder = sortOrder;
        IsActive = true;
        IsCalculated = isCalculated;
    }

    /// <summary>
    /// Fails fast at creation time rather than letting an incomplete config
    /// reach an evaluator later and fail confusingly mid-evaluation.
    /// </summary>
    private static void ValidateConfig(EvaluationStrategy strategy, EvaluationConfig config)
    {
        switch (strategy)
        {
            case EvaluationStrategy.StayAbove:
            case EvaluationStrategy.StayBelow:
                if (config.Threshold is null)
                {
                    throw new ArgumentException(
                        $"{strategy} requires {nameof(EvaluationConfig.Threshold)}.", nameof(config));
                }

                break;

            case EvaluationStrategy.StayWithinRange:
                if (config.MinValue is null || config.MaxValue is null)
                {
                    throw new ArgumentException(
                        $"{EvaluationStrategy.StayWithinRange} requires " +
                        $"{nameof(EvaluationConfig.MinValue)} and {nameof(EvaluationConfig.MaxValue)}.",
                        nameof(config));
                }

                if (config.MinValue >= config.MaxValue)
                {
                    throw new ArgumentException(
                        $"{nameof(EvaluationConfig.MinValue)} must be less than " +
                        $"{nameof(EvaluationConfig.MaxValue)}.", nameof(config));
                }

                break;

            case EvaluationStrategy.Increase:
            case EvaluationStrategy.Decrease:
                // No parameters required.
                break;
        }
    }
}
