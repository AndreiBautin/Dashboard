/**
 * The shape of every payload the app consumes.
 *
 * These types are the contract between the React app and *both* data
 * adapters. `Vantage.Api`'s controllers and `Vantage.Wasm`'s [JSExport]
 * façade serialize the same Application-layer records with the same naming
 * policy and the same string-enum handling, so one set of types describes
 * both. If the two ever diverge this file becomes a lie — which is exactly
 * why the demo façade serializes the real DTOs rather than hand-built
 * look-alikes.
 */

export type CategoryStatus = "Struggling" | "NeedsAttention" | "OnTrack" | "Excelling" | "NoData";
export type MetricStatus = "Improved" | "Regressed" | "Stagnant" | "InsufficientData";

export interface MetricSummary {
  metricDefinitionId: number;
  metricName: string;
  status: MetricStatus;
  score: number | null;
}

export interface CategorySummary {
  categoryId: number;
  categoryName: string;
  status: CategoryStatus;
  score: number | null;
  metrics: MetricSummary[];
}

export interface DashboardAlert {
  metricDefinitionId: number;
  metricName: string;
  categoryName: string;
  message: string;
  score: number | null;
  status: CategoryStatus;
}

export interface DashboardSummary {
  overallScore: number | null;
  overallStatus: CategoryStatus;
  categories: CategorySummary[];
  alerts: DashboardAlert[];
}

export interface MetricTrendPoint {
  month: string;
  value: number;
}

export interface Category {
  id: number;
  name: string;
  sortOrder: number;
}

export type MetricRatingTier = "Tier1" | "Tier2" | "Tier3" | "Tier4";

export interface RatingBand {
  upToValue: number | null;
  label: string;
}

export interface MetricDetail {
  metricDefinitionId: number;
  metricName: string;
  unit: string;
  latestValue: number | null;
  currentMonthValue: number | null;
  status: MetricStatus;
  ratingLabel: string | null;
  ratingTier: MetricRatingTier | null;
  ratingDescription: string | null;
  score: number | null;
  isCalculated: boolean;
  ratingBands: RatingBand[] | null;
}

export interface CategoryDetail {
  categoryId: number;
  categoryName: string;
  score: number | null;
  status: CategoryStatus;
  metrics: MetricDetail[];
}

export interface FriendSummary {
  friendId: number;
  name: string;
  notes: string | null;
  lastHangoutDate: string;
  daysSinceLastHangout: number;
  isActive: boolean;
  isFlaggedOverdue: boolean;
}

export type SocialCircleRating = "Thin" | "Healthy" | "Robust" | "Expansive";

export type KeyRelationshipKind = "DateWithWife" | "VisitedMother";

export interface KeyRelationshipSummary {
  keyRelationshipId: number;
  kind: KeyRelationshipKind;
  label: string;
  lastContactDate: string;
  daysSinceLastContact: number;
  isFlaggedOverdue: boolean;
  score: number;
}

export interface SocialSummary {
  activeFriendCount: number;
  activeFriendCountDelta: number | null;
  rating: SocialCircleRating;
  ratingDescription: string;
  ratingScore: number;
  maintenanceScore: number | null;
  keyRelationships: KeyRelationshipSummary[];
  score: number;
  status: CategoryStatus;
  friends: FriendSummary[];
  ratingBands: RatingBand[];
}

export interface AppSettingSummary {
  key: string;
  section: string;
  label: string;
  description: string;
  value: string;
  defaultValue: string;
}
