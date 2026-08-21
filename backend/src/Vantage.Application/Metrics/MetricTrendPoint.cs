namespace Vantage.Application.Metrics;

/// <summary>One month's value for a metric, ordered oldest to newest for charting.</summary>
public sealed record MetricTrendPoint(DateOnly Month, decimal Value);
