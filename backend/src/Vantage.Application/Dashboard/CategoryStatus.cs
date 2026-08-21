namespace Vantage.Application.Dashboard;

/// <summary>
/// The graduated, four-tier qualitative read of a 0-100 score (plus NoData
/// for when there's nothing to score yet) -- mirrors the same "gradient"
/// treatment Social's circle-size rating already gets (Thin/Healthy/Robust/
/// Expansive), rather than a flat good/bad split. Order matters: it's used
/// to render worst-to-best in tier indicators.
/// </summary>
public enum CategoryStatus
{
    Struggling,
    NeedsAttention,
    OnTrack,
    Excelling,

    /// <summary>No metrics yet, or none with enough history to evaluate.</summary>
    NoData,
}
