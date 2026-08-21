namespace Vantage.Domain.Social;

/// <summary>
/// A qualitative read on active circle size, on top of the raw count --
/// e.g. "9 active friends" doesn't mean much without a sense of whether
/// that's thin, healthy, or thriving. See <see cref="SocialCircleRatingCalculator"/>
/// for the thresholds.
/// </summary>
public enum SocialCircleRating
{
    /// <summary>Small enough that losing one person would be felt immediately.</summary>
    Thin,

    /// <summary>A solid, sustainable circle size.</summary>
    Healthy,

    /// <summary>A large, well-maintained circle.</summary>
    Robust,

    /// <summary>Typically only sustained by a very social lifestyle or a community-based hobby.</summary>
    Expansive,
}
