using System.Runtime.CompilerServices;
using Dashboard.Domain.Metrics;
using Dashboard.Domain.Social;

namespace Dashboard.Demo;

/// <summary>
/// Assigns the surrogate keys that EF Core would normally assign.
///
/// Every entity in <c>Dashboard.Domain</c> keeps <c>Id</c> as a
/// <c>private set</c> so that nothing outside the aggregate can forge or
/// reassign an identity — which is exactly the right design, and exactly
/// what an in-memory store has to work around. Reflection would do it, but
/// reflection over a private setter is precisely the pattern the WebAssembly
/// trimmer cannot see and will happily strip.
///
/// <see cref="UnsafeAccessorAttribute"/> is the trim-safe alternative: the
/// binding is resolved at compile time into a direct call, so the trimmer
/// keeps the setter and there is no runtime reflection at all. The domain
/// model keeps its encapsulation, and this file is the single, named place
/// where that encapsulation is deliberately stepped around.
/// </summary>
internal static class EntityIdentity
{
    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetCategoryId(Category target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetMetricDefinitionId(MetricDefinition target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetMonthlySnapshotId(MonthlySnapshot target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetMetricSnapshotId(MetricSnapshot target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_MonthlySnapshotId")]
    private static extern void SetMetricSnapshotParentId(MetricSnapshot target, int monthlySnapshotId);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetFriendId(Friend target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetKeyRelationshipId(KeyRelationship target, int id);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
    private static extern void SetSocialSnapshotId(SocialSnapshot target, int id);

    internal static Category WithId(this Category target, int id)
    {
        SetCategoryId(target, id);
        return target;
    }

    internal static MetricDefinition WithId(this MetricDefinition target, int id)
    {
        SetMetricDefinitionId(target, id);
        return target;
    }

    internal static MonthlySnapshot WithId(this MonthlySnapshot target, int id)
    {
        SetMonthlySnapshotId(target, id);
        return target;
    }

    internal static MetricSnapshot WithId(this MetricSnapshot target, int id, int monthlySnapshotId)
    {
        SetMetricSnapshotId(target, id);
        SetMetricSnapshotParentId(target, monthlySnapshotId);
        return target;
    }

    internal static Friend WithId(this Friend target, int id)
    {
        SetFriendId(target, id);
        return target;
    }

    internal static KeyRelationship WithId(this KeyRelationship target, int id)
    {
        SetKeyRelationshipId(target, id);
        return target;
    }

    internal static SocialSnapshot WithId(this SocialSnapshot target, int id)
    {
        SetSocialSnapshotId(target, id);
        return target;
    }
}
