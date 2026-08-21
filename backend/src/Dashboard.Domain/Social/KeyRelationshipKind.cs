namespace Dashboard.Domain.Social;

/// <summary>
/// The fixed set of "important relationships" tracked outside the regular
/// Friends list -- unlike Friends (an open-ended, user-grown list that also
/// feeds the active-circle size rating), these are a small, known set of
/// specific people who each get their own overdue check, configured
/// independently (see KnownAppSettings' DateWithWifeThresholdMonths /
/// VisitedMotherThresholdMonths) and excluded from circle-size math.
/// </summary>
public enum KeyRelationshipKind
{
    DateWithWife,
    VisitedMother,
}
