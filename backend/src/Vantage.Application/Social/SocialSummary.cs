using Vantage.Application.Dashboard;
using Vantage.Application.Metrics;
using Vantage.Domain.Social;

namespace Vantage.Application.Social;

/// <summary>
/// <paramref name="ActiveFriendCountDelta"/> is null when there's no prior
/// month's captured SocialSnapshot to compare against (e.g. the very first
/// review) -- same "no data yet" shape as the rest of the app rather than a
/// misleading zero. <paramref name="Rating"/> is a qualitative read on
/// <paramref name="ActiveFriendCount"/> per the configurable size bands in
/// SocialCircleRatingCalculator, and <paramref name="RatingDescription"/> is
/// a longer, configurable blurb for that rating (see KnownAppSettings'
/// SocialTier1-4Description). <paramref name="RatingScore"/> is that same
/// size rating as a continuous 0-100 number (see
/// SocialCircleRatingCalculator.RateContinuous) rather than just the 4-tier
/// label -- always defined, since even 0 active friends is a valid (if low)
/// point on the scale, not "no data". <paramref name="MaintenanceScore"/> is
/// orthogonal to the size rating: it's the percent of the active circle that
/// ISN'T flagged overdue, i.e. how well you're keeping up with the people
/// you have, regardless of how many of them there are -- null (not 0) when
/// there's no active circle yet to maintain. <paramref name="KeyRelationships"/>
/// covers the fixed "important relationship" check-ins (Date with Wife,
/// Visited Mother) tracked outside the regular Friends list -- each
/// contributes its own flat 100/30 score. <paramref name="Score"/> and
/// <paramref name="Status"/> are Social's single blended read across all of
/// the above (RatingScore, MaintenanceScore when present, and every key
/// relationship), computed once here rather than recomputed by every
/// consumer -- the Dashboard, this page's own header, and anywhere else
/// Social's score is shown all read the same two fields.
/// <paramref name="RatingBands"/> is the Active Circle size scale in full --
/// every cutoff paired with its label (Thin/Healthy/Robust/Expansive) --
/// same "show the whole scale, not just the current tier" treatment
/// <see cref="RatingBand"/> gives rated metrics on the category detail
/// pages. Always defined (unlike a per-metric rating), since circle size
/// always has a rating.
/// </summary>
public sealed record SocialSummary(
    int ActiveFriendCount,
    int? ActiveFriendCountDelta,
    SocialCircleRating Rating,
    string RatingDescription,
    int RatingScore,
    int? MaintenanceScore,
    IReadOnlyList<KeyRelationshipSummary> KeyRelationships,
    int Score,
    CategoryStatus Status,
    IReadOnlyList<FriendSummary> Friends,
    IReadOnlyList<RatingBand> RatingBands);
