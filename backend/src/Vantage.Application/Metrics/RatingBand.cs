namespace Vantage.Application.Metrics;

/// <summary>
/// One row of a rated metric's full threshold breakdown, in ascending order
/// by the metric's own raw value (dollars, pounds, inches, ...) -- e.g. "up
/// to $15,000: Building". <paramref name="UpToValue"/> is null for the last
/// row (the open-ended top of the scale, e.g. "above $30,000: Well Funded").
/// Exists so the Settings page's threshold values and a metric's rating
/// label aren't the only way to piece together "what does it actually take
/// to reach the next tier" -- the whole scale is visible right on the
/// category detail page, next to the metric it describes.
/// </summary>
public sealed record RatingBand(decimal? UpToValue, string Label);
