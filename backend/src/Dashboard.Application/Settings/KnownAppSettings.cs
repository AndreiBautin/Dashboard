namespace Dashboard.Application.Settings;

/// <summary>
/// The single registry of every setting the app knows how to display and
/// edit on the Settings page. Adding a new configurable value anywhere in
/// the app means adding one entry here and reading it via SettingsService/
/// IAppSettingRepository -- the Settings page itself needs no code change.
/// </summary>
public static class KnownAppSettings
{
    // -- Graduated category/overall status tiers (Struggling/NeedsAttention/OnTrack/Excelling) --

    public static readonly AppSettingDefinition CategoryStatusStrugglingMax = new(
        Key: "CategoryStatusStrugglingMax",
        Section: "General",
        Label: "\"Struggling\" score up to",
        Description: "A category's or the overall score at or below this is rated Struggling. Aligned with MetricRatingCalculator.RateContinuous's Tier1 band (0-25) so a metric's graduated tier (Beginner/Advanced/etc.) never disagrees with its score-based status.",
        DefaultValue: "25");

    public static readonly AppSettingDefinition CategoryStatusNeedsAttentionMax = new(
        Key: "CategoryStatusNeedsAttentionMax",
        Section: "General",
        Label: "\"Needs Attention\" score up to",
        Description: "Above the Struggling cutoff, up to this, is rated Needs Attention. Aligned with RateContinuous's Tier2 band (25-50).",
        DefaultValue: "50");

    public static readonly AppSettingDefinition CategoryStatusOnTrackMax = new(
        Key: "CategoryStatusOnTrackMax",
        Section: "General",
        Label: "\"On Track\" score up to",
        Description: "Above the Needs Attention cutoff, up to this, is rated On Track. Anything higher is rated Excelling. Aligned with RateContinuous's Tier3 band (50-75), so Tier4 (Elite/flat 100) always reads as Excelling.",
        DefaultValue: "75");

    public static readonly AppSettingDefinition ActiveCircleThresholdMonths = new(
        Key: "ActiveCircleThresholdMonths",
        Section: "Social",
        Label: "Active circle window (months)",
        Description: "How recently you need to have hung out with someone for them to still count toward your active circle.",
        DefaultValue: "12");

    public static readonly AppSettingDefinition SocialCircleThinMax = new(
        Key: "SocialCircleThinMax",
        Section: "Social",
        Label: "\"Thin\" circle size (up to)",
        Description: "Active friend counts at or below this are rated Thin.",
        DefaultValue: "4");

    public static readonly AppSettingDefinition SocialCircleHealthyMax = new(
        Key: "SocialCircleHealthyMax",
        Section: "Social",
        Label: "\"Healthy\" circle size (up to)",
        Description: "Active friend counts above the Thin cutoff, up to this, are rated Healthy.",
        DefaultValue: "8");

    public static readonly AppSettingDefinition SocialCircleRobustMax = new(
        Key: "SocialCircleRobustMax",
        Section: "Social",
        Label: "\"Robust\" circle size (up to)",
        Description: "Active friend counts above the Healthy cutoff, up to this, are rated Robust. Anything higher is rated Expansive.",
        DefaultValue: "14");

    public static readonly AppSettingDefinition OverdueThresholdMonths = new(
        Key: "OverdueThresholdMonths",
        Section: "Social",
        Label: "Overdue window (months)",
        Description: "A friend is flagged overdue once it's been more than this many months since your last hangout.",
        DefaultValue: "3");

    public static readonly AppSettingDefinition DateWithWifeThresholdMonths = new(
        Key: "DateWithWifeThresholdMonths",
        Section: "Social",
        Label: "Date with Wife window (months)",
        Description: "Flagged overdue once it's been more than this many months since your last date with your wife.",
        DefaultValue: "1");

    public static readonly AppSettingDefinition VisitedMotherThresholdMonths = new(
        Key: "VisitedMotherThresholdMonths",
        Section: "Social",
        Label: "Visited Mother window (months)",
        Description: "Flagged overdue once it's been more than this many months since you last visited your mother.",
        DefaultValue: "1");

    public static readonly AppSettingDefinition SocialTier1Description = new(
        Key: "SocialTier1Description",
        Section: "Social",
        Label: "\"Thin\" rating description",
        Description: "Shown under your active circle count when it's rated Thin.",
        DefaultValue: "Losing one person would be felt immediately.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SocialTier2Description = new(
        Key: "SocialTier2Description",
        Section: "Social",
        Label: "\"Healthy\" rating description",
        Description: "Shown under your active circle count when it's rated Healthy.",
        DefaultValue: "A solid, sustainable circle size.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SocialTier3Description = new(
        Key: "SocialTier3Description",
        Section: "Social",
        Label: "\"Robust\" rating description",
        Description: "Shown under your active circle count when it's rated Robust.",
        DefaultValue: "A large, well-maintained circle.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SocialTier4Description = new(
        Key: "SocialTier4Description",
        Section: "Social",
        Label: "\"Expansive\" rating description",
        Description: "Shown under your active circle count when it's rated Expansive.",
        DefaultValue: "Usually means a very social lifestyle or a community-based hobby.",
        ValueKind: AppSettingValueKind.Text);

    // -- Net Worth rating (Finance) --

    public static readonly AppSettingDefinition NetWorthTier1Max = new(
        Key: "NetWorthTier1Max",
        Section: "Finance",
        Label: "Net Worth \"Building\" up to",
        Description: "Net Worth at or below this is rated Building.",
        DefaultValue: "50000");

    public static readonly AppSettingDefinition NetWorthTier2Max = new(
        Key: "NetWorthTier2Max",
        Section: "Finance",
        Label: "Net Worth \"Growing\" up to",
        Description: "Net Worth above the Building cutoff, up to this, is rated Growing.",
        DefaultValue: "250000");

    public static readonly AppSettingDefinition NetWorthTier3Max = new(
        Key: "NetWorthTier3Max",
        Section: "Finance",
        Label: "Net Worth \"Strong\" up to",
        Description: "Net Worth above the Growing cutoff, up to this, is rated Strong. Anything higher is rated Thriving.",
        DefaultValue: "750000");

    public static readonly AppSettingDefinition NetWorthTier1Description = new(
        Key: "NetWorthTier1Description",
        Section: "Finance",
        Label: "Net Worth \"Building\" description",
        Description: "Shown under Net Worth when it's rated Building.",
        DefaultValue: "Early stage -- the foundation is still being laid.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition NetWorthTier2Description = new(
        Key: "NetWorthTier2Description",
        Section: "Finance",
        Label: "Net Worth \"Growing\" description",
        Description: "Shown under Net Worth when it's rated Growing.",
        DefaultValue: "Steady progress with real momentum building.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition NetWorthTier3Description = new(
        Key: "NetWorthTier3Description",
        Section: "Finance",
        Label: "Net Worth \"Strong\" description",
        Description: "Shown under Net Worth when it's rated Strong.",
        DefaultValue: "A solid financial cushion, with room to take bigger swings.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition NetWorthTier4Description = new(
        Key: "NetWorthTier4Description",
        Section: "Finance",
        Label: "Net Worth \"Thriving\" description",
        Description: "Shown under Net Worth when it's rated Thriving.",
        DefaultValue: "Well ahead of where most people ever get to.",
        ValueKind: AppSettingValueKind.Text);

    // -- Credit Score rating (Finance) --

    public static readonly AppSettingDefinition CreditScoreTier1Max = new(
        Key: "CreditScoreTier1Max",
        Section: "Finance",
        Label: "Credit Score \"Poor\" up to",
        Description: "Credit Score at or below this is rated Poor.",
        DefaultValue: "579");

    public static readonly AppSettingDefinition CreditScoreTier2Max = new(
        Key: "CreditScoreTier2Max",
        Section: "Finance",
        Label: "Credit Score \"Fair\" up to",
        Description: "Credit Score above the Poor cutoff, up to this, is rated Fair.",
        DefaultValue: "669");

    public static readonly AppSettingDefinition CreditScoreTier3Max = new(
        Key: "CreditScoreTier3Max",
        Section: "Finance",
        Label: "Credit Score \"Good\" up to",
        Description: "Credit Score above the Fair cutoff, up to this, is rated Good. Anything higher is rated Excellent.",
        DefaultValue: "739");

    public static readonly AppSettingDefinition CreditScoreTier1Description = new(
        Key: "CreditScoreTier1Description",
        Section: "Finance",
        Label: "Credit Score \"Poor\" description",
        Description: "Shown under Credit Score when it's rated Poor.",
        DefaultValue: "Likely to face higher rates or denials on credit.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition CreditScoreTier2Description = new(
        Key: "CreditScoreTier2Description",
        Section: "Finance",
        Label: "Credit Score \"Fair\" description",
        Description: "Shown under Credit Score when it's rated Fair.",
        DefaultValue: "Approvable for most credit, but not at the best rates.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition CreditScoreTier3Description = new(
        Key: "CreditScoreTier3Description",
        Section: "Finance",
        Label: "Credit Score \"Good\" description",
        Description: "Shown under Credit Score when it's rated Good.",
        DefaultValue: "Qualifies for most favorable rates and terms.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition CreditScoreTier4Description = new(
        Key: "CreditScoreTier4Description",
        Section: "Finance",
        Label: "Credit Score \"Excellent\" description",
        Description: "Shown under Credit Score when it's rated Excellent.",
        DefaultValue: "Top-tier -- the best rates and terms are available.",
        ValueKind: AppSettingValueKind.Text);

    // -- Emergency Fund rating (Finance) -- rough 1/3/6-month-of-expenses
    // milestones, the standard emergency-fund framework.

    public static readonly AppSettingDefinition EmergencyFundTier1Max = new(
        Key: "EmergencyFundTier1Max",
        Section: "Finance",
        Label: "Emergency Fund \"Starting\" up to",
        Description: "Emergency Fund at or below this is rated Starting.",
        DefaultValue: "5000");

    public static readonly AppSettingDefinition EmergencyFundTier2Max = new(
        Key: "EmergencyFundTier2Max",
        Section: "Finance",
        Label: "Emergency Fund \"Building\" up to",
        Description: "Emergency Fund above the Starting cutoff, up to this, is rated Building.",
        DefaultValue: "15000");

    public static readonly AppSettingDefinition EmergencyFundTier3Max = new(
        Key: "EmergencyFundTier3Max",
        Section: "Finance",
        Label: "Emergency Fund \"Almost There\" up to",
        Description: "Emergency Fund above the Building cutoff, up to this, is rated Almost There. Anything higher is rated Well Funded.",
        DefaultValue: "30000");

    public static readonly AppSettingDefinition EmergencyFundTier1Description = new(
        Key: "EmergencyFundTier1Description",
        Section: "Finance",
        Label: "Emergency Fund \"Starting\" description",
        Description: "Shown under Emergency Fund when it's rated Starting.",
        DefaultValue: "Less than a month of expenses covered so far.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition EmergencyFundTier2Description = new(
        Key: "EmergencyFundTier2Description",
        Section: "Finance",
        Label: "Emergency Fund \"Building\" description",
        Description: "Shown under Emergency Fund when it's rated Building.",
        DefaultValue: "Roughly one to three months of expenses covered.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition EmergencyFundTier3Description = new(
        Key: "EmergencyFundTier3Description",
        Section: "Finance",
        Label: "Emergency Fund \"Almost There\" description",
        Description: "Shown under Emergency Fund when it's rated Almost There.",
        DefaultValue: "Roughly three to six months of expenses covered.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition EmergencyFundTier4Description = new(
        Key: "EmergencyFundTier4Description",
        Section: "Finance",
        Label: "Emergency Fund \"Well Funded\" description",
        Description: "Shown under Emergency Fund when it's rated Well Funded.",
        DefaultValue: "Six or more months of expenses covered -- a solid safety net.",
        ValueKind: AppSettingValueKind.Text);

    // -- Retirement Fund rating (Finance) --

    public static readonly AppSettingDefinition RetirementFundTier1Max = new(
        Key: "RetirementFundTier1Max",
        Section: "Finance",
        Label: "Retirement Fund \"Starting\" up to",
        Description: "Retirement Fund at or below this is rated Starting.",
        DefaultValue: "25000");

    public static readonly AppSettingDefinition RetirementFundTier2Max = new(
        Key: "RetirementFundTier2Max",
        Section: "Finance",
        Label: "Retirement Fund \"Building\" up to",
        Description: "Retirement Fund above the Starting cutoff, up to this, is rated Building.",
        DefaultValue: "100000");

    public static readonly AppSettingDefinition RetirementFundTier3Max = new(
        Key: "RetirementFundTier3Max",
        Section: "Finance",
        Label: "Retirement Fund \"Strong\" up to",
        Description: "Retirement Fund above the Building cutoff, up to this, is rated Strong. Anything higher is rated Thriving.",
        DefaultValue: "500000");

    public static readonly AppSettingDefinition RetirementFundTier1Description = new(
        Key: "RetirementFundTier1Description",
        Section: "Finance",
        Label: "Retirement Fund \"Starting\" description",
        Description: "Shown under Retirement Fund when it's rated Starting.",
        DefaultValue: "Early stage -- just getting off the ground.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition RetirementFundTier2Description = new(
        Key: "RetirementFundTier2Description",
        Section: "Finance",
        Label: "Retirement Fund \"Building\" description",
        Description: "Shown under Retirement Fund when it's rated Building.",
        DefaultValue: "Steady progress with real momentum building.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition RetirementFundTier3Description = new(
        Key: "RetirementFundTier3Description",
        Section: "Finance",
        Label: "Retirement Fund \"Strong\" description",
        Description: "Shown under Retirement Fund when it's rated Strong.",
        DefaultValue: "Well ahead of pace for a comfortable retirement.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition RetirementFundTier4Description = new(
        Key: "RetirementFundTier4Description",
        Section: "Finance",
        Label: "Retirement Fund \"Thriving\" description",
        Description: "Shown under Retirement Fund when it's rated Thriving.",
        DefaultValue: "Well ahead of where most people ever get to.",
        ValueKind: AppSettingValueKind.Text);

    // -- Strength Total rating (Fitness) --

    public static readonly AppSettingDefinition StrengthTotalTier1Max = new(
        Key: "StrengthTotalTier1Max",
        Section: "Fitness",
        Label: "Strength Total \"Beginner\" up to (lb)",
        Description: "Squat + Bench Press + Deadlift + Overhead Press at or below this is rated Beginner. Kept in sync with the sum of the four individual lifts' own Beginner cutoffs (175+160+250+110).",
        DefaultValue: "695");

    public static readonly AppSettingDefinition StrengthTotalTier2Max = new(
        Key: "StrengthTotalTier2Max",
        Section: "Fitness",
        Label: "Strength Total \"Intermediate\" up to (lb)",
        Description: "Above the Beginner cutoff, up to this, is rated Intermediate. Kept in sync with the sum of the four individual lifts' own Intermediate cutoffs (290+230+375+160).",
        DefaultValue: "1055");

    public static readonly AppSettingDefinition StrengthTotalTier3Max = new(
        Key: "StrengthTotalTier3Max",
        Section: "Fitness",
        Label: "Strength Total \"Advanced\" up to (lb)",
        Description: "Above the Intermediate cutoff, up to this, is rated Advanced. Anything higher is rated Elite. Kept in sync with the sum of the four individual lifts' own Advanced cutoffs (405+315+495+225) -- a 4-plate squat + 3-plate bench + 5-plate deadlift + 2-plate OHP.",
        DefaultValue: "1440");

    public static readonly AppSettingDefinition StrengthTotalTier1Description = new(
        Key: "StrengthTotalTier1Description",
        Section: "Fitness",
        Label: "Strength Total \"Beginner\" description",
        Description: "Shown under Strength Total when it's rated Beginner.",
        DefaultValue: "Just getting started -- technique and consistency matter most here.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition StrengthTotalTier2Description = new(
        Key: "StrengthTotalTier2Description",
        Section: "Fitness",
        Label: "Strength Total \"Intermediate\" description",
        Description: "Shown under Strength Total when it's rated Intermediate.",
        DefaultValue: "Real strength built, with a solid base to keep progressing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition StrengthTotalTier3Description = new(
        Key: "StrengthTotalTier3Description",
        Section: "Fitness",
        Label: "Strength Total \"Advanced\" description",
        Description: "Shown under Strength Total when it's rated Advanced.",
        DefaultValue: "Well above the average lifter, built from years of consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition StrengthTotalTier4Description = new(
        Key: "StrengthTotalTier4Description",
        Section: "Fitness",
        Label: "Strength Total \"Elite\" description",
        Description: "Shown under Strength Total when it's rated Elite.",
        DefaultValue: "Among the strongest lifters, natural or not.",
        ValueKind: AppSettingValueKind.Text);

    // -- Arm Measurement rating (Fitness) --

    public static readonly AppSettingDefinition ArmMeasurementTier1Max = new(
        Key: "ArmMeasurementTier1Max",
        Section: "Fitness",
        Label: "Arm Measurement \"Average\" up to (in)",
        Description: "Flexed arm measurement at or below this is rated Average.",
        DefaultValue: "13",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition ArmMeasurementTier2Max = new(
        Key: "ArmMeasurementTier2Max",
        Section: "Fitness",
        Label: "Arm Measurement \"Developed\" up to (in)",
        Description: "Above the Average cutoff, up to this, is rated Developed.",
        DefaultValue: "15",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition ArmMeasurementTier3Max = new(
        Key: "ArmMeasurementTier3Max",
        Section: "Fitness",
        Label: "Arm Measurement \"Big\" up to (in)",
        Description: "Above the Developed cutoff, up to this, is rated Big. Anything higher is rated Exceptional.",
        DefaultValue: "17",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition ArmMeasurementTier1Description = new(
        Key: "ArmMeasurementTier1Description",
        Section: "Fitness",
        Label: "Arm Measurement \"Average\" description",
        Description: "Shown under Arm Measurement when it's rated Average.",
        DefaultValue: "Typical for someone who isn't specifically training arms.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition ArmMeasurementTier2Description = new(
        Key: "ArmMeasurementTier2Description",
        Section: "Fitness",
        Label: "Arm Measurement \"Developed\" description",
        Description: "Shown under Arm Measurement when it's rated Developed.",
        DefaultValue: "Noticeably built from consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition ArmMeasurementTier3Description = new(
        Key: "ArmMeasurementTier3Description",
        Section: "Fitness",
        Label: "Arm Measurement \"Big\" description",
        Description: "Shown under Arm Measurement when it's rated Big.",
        DefaultValue: "Stands out -- clearly a focus of training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition ArmMeasurementTier4Description = new(
        Key: "ArmMeasurementTier4Description",
        Section: "Fitness",
        Label: "Arm Measurement \"Exceptional\" description",
        Description: "Shown under Arm Measurement when it's rated Exceptional.",
        DefaultValue: "Rare size, typically requiring years of dedicated work.",
        ValueKind: AppSettingValueKind.Text);

    // -- VO2 Max rating (Fitness) --

    public static readonly AppSettingDefinition Vo2MaxTier1Max = new(
        Key: "Vo2MaxTier1Max",
        Section: "Fitness",
        Label: "VO2 Max \"Below Average\" up to (ml/kg/min)",
        Description: "VO2 Max at or below this is rated Below Average.",
        DefaultValue: "35",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition Vo2MaxTier2Max = new(
        Key: "Vo2MaxTier2Max",
        Section: "Fitness",
        Label: "VO2 Max \"Average\" up to (ml/kg/min)",
        Description: "Above the Below Average cutoff, up to this, is rated Average.",
        DefaultValue: "42",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition Vo2MaxTier3Max = new(
        Key: "Vo2MaxTier3Max",
        Section: "Fitness",
        Label: "VO2 Max \"Good\" up to (ml/kg/min)",
        Description: "Above the Average cutoff, up to this, is rated Good. Anything higher is rated Excellent.",
        DefaultValue: "50",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition Vo2MaxTier1Description = new(
        Key: "Vo2MaxTier1Description",
        Section: "Fitness",
        Label: "VO2 Max \"Below Average\" description",
        Description: "Shown under VO2 Max when it's rated Below Average.",
        DefaultValue: "Aerobic capacity has real room to grow.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition Vo2MaxTier2Description = new(
        Key: "Vo2MaxTier2Description",
        Section: "Fitness",
        Label: "VO2 Max \"Average\" description",
        Description: "Shown under VO2 Max when it's rated Average.",
        DefaultValue: "Typical aerobic fitness for an active adult.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition Vo2MaxTier3Description = new(
        Key: "Vo2MaxTier3Description",
        Section: "Fitness",
        Label: "VO2 Max \"Good\" description",
        Description: "Shown under VO2 Max when it's rated Good.",
        DefaultValue: "Solid cardiovascular fitness from consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition Vo2MaxTier4Description = new(
        Key: "Vo2MaxTier4Description",
        Section: "Fitness",
        Label: "VO2 Max \"Excellent\" description",
        Description: "Shown under VO2 Max when it's rated Excellent.",
        DefaultValue: "Elite-level aerobic capacity.",
        ValueKind: AppSettingValueKind.Text);

    // -- Waist Measurement rating (Fitness) -- lower is better, so Tier1 here
    // is the smallest/healthiest band rather than the worst one, the reverse
    // of every other rated metric.

    public static readonly AppSettingDefinition WaistMeasurementTier1Max = new(
        Key: "WaistMeasurementTier1Max",
        Section: "Fitness",
        Label: "Waist Measurement \"Lean\" up to (in)",
        Description: "Waist measurement at or below this is rated Lean (the healthiest band).",
        DefaultValue: "32",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition WaistMeasurementTier2Max = new(
        Key: "WaistMeasurementTier2Max",
        Section: "Fitness",
        Label: "Waist Measurement \"Trim\" up to (in)",
        Description: "Above the Lean cutoff, up to this, is rated Trim.",
        DefaultValue: "36",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition WaistMeasurementTier3Max = new(
        Key: "WaistMeasurementTier3Max",
        Section: "Fitness",
        Label: "Waist Measurement \"Elevated\" up to (in)",
        Description: "Above the Trim cutoff, up to this, is rated Elevated. Anything higher is rated High.",
        DefaultValue: "40",
        ValueKind: AppSettingValueKind.Decimal);

    public static readonly AppSettingDefinition WaistMeasurementTier1Description = new(
        Key: "WaistMeasurementTier1Description",
        Section: "Fitness",
        Label: "Waist Measurement \"High\" description",
        Description: "Shown under Waist Measurement when it's rated High -- the least healthy band.",
        DefaultValue: "Meaningfully above a healthy range -- worth prioritizing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition WaistMeasurementTier2Description = new(
        Key: "WaistMeasurementTier2Description",
        Section: "Fitness",
        Label: "Waist Measurement \"Elevated\" description",
        Description: "Shown under Waist Measurement when it's rated Elevated.",
        DefaultValue: "A bit above a healthy range.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition WaistMeasurementTier3Description = new(
        Key: "WaistMeasurementTier3Description",
        Section: "Fitness",
        Label: "Waist Measurement \"Trim\" description",
        Description: "Shown under Waist Measurement when it's rated Trim.",
        DefaultValue: "Within a healthy range.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition WaistMeasurementTier4Description = new(
        Key: "WaistMeasurementTier4Description",
        Section: "Fitness",
        Label: "Waist Measurement \"Lean\" description",
        Description: "Shown under Waist Measurement when it's rated Lean -- the healthiest band.",
        DefaultValue: "At the lean end of a healthy range.",
        ValueKind: AppSettingValueKind.Text);

    // -- Squat rating (Fitness) --

    public static readonly AppSettingDefinition SquatTier1Max = new(
        Key: "SquatTier1Max",
        Section: "Fitness",
        Label: "Squat \"Beginner\" up to (lb)",
        Description: "Squat at or below this is rated Beginner.",
        DefaultValue: "175");

    public static readonly AppSettingDefinition SquatTier2Max = new(
        Key: "SquatTier2Max",
        Section: "Fitness",
        Label: "Squat \"Intermediate\" up to (lb)",
        Description: "Above the Beginner cutoff, up to this, is rated Intermediate.",
        DefaultValue: "290");

    public static readonly AppSettingDefinition SquatTier3Max = new(
        Key: "SquatTier3Max",
        Section: "Fitness",
        Label: "Squat \"Advanced\" up to (lb)",
        Description: "Above the Intermediate cutoff, up to this, is rated Advanced. Anything higher is rated Elite -- a 4-plate squat (405 lb: bar + 4x45 lb plates per side).",
        DefaultValue: "405");

    public static readonly AppSettingDefinition SquatTier1Description = new(
        Key: "SquatTier1Description",
        Section: "Fitness",
        Label: "Squat \"Beginner\" description",
        Description: "Shown under Squat when it's rated Beginner.",
        DefaultValue: "Just getting started -- technique and consistency matter most here.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SquatTier2Description = new(
        Key: "SquatTier2Description",
        Section: "Fitness",
        Label: "Squat \"Intermediate\" description",
        Description: "Shown under Squat when it's rated Intermediate.",
        DefaultValue: "Real strength built, with a solid base to keep progressing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SquatTier3Description = new(
        Key: "SquatTier3Description",
        Section: "Fitness",
        Label: "Squat \"Advanced\" description",
        Description: "Shown under Squat when it's rated Advanced.",
        DefaultValue: "Well above the average lifter, built from years of consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition SquatTier4Description = new(
        Key: "SquatTier4Description",
        Section: "Fitness",
        Label: "Squat \"Elite\" description",
        Description: "Shown under Squat when it's rated Elite.",
        DefaultValue: "Among the strongest lifters, natural or not.",
        ValueKind: AppSettingValueKind.Text);

    // -- Bench Press rating (Fitness) --

    public static readonly AppSettingDefinition BenchPressTier1Max = new(
        Key: "BenchPressTier1Max",
        Section: "Fitness",
        Label: "Bench Press \"Beginner\" up to (lb)",
        Description: "Bench Press at or below this is rated Beginner.",
        DefaultValue: "160");

    public static readonly AppSettingDefinition BenchPressTier2Max = new(
        Key: "BenchPressTier2Max",
        Section: "Fitness",
        Label: "Bench Press \"Intermediate\" up to (lb)",
        Description: "Above the Beginner cutoff, up to this, is rated Intermediate.",
        DefaultValue: "230");

    public static readonly AppSettingDefinition BenchPressTier3Max = new(
        Key: "BenchPressTier3Max",
        Section: "Fitness",
        Label: "Bench Press \"Advanced\" up to (lb)",
        Description: "Above the Intermediate cutoff, up to this, is rated Advanced. Anything higher is rated Elite -- a 3-plate bench (315 lb: bar + 3x45 lb plates per side).",
        DefaultValue: "315");

    public static readonly AppSettingDefinition BenchPressTier1Description = new(
        Key: "BenchPressTier1Description",
        Section: "Fitness",
        Label: "Bench Press \"Beginner\" description",
        Description: "Shown under Bench Press when it's rated Beginner.",
        DefaultValue: "Just getting started -- technique and consistency matter most here.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition BenchPressTier2Description = new(
        Key: "BenchPressTier2Description",
        Section: "Fitness",
        Label: "Bench Press \"Intermediate\" description",
        Description: "Shown under Bench Press when it's rated Intermediate.",
        DefaultValue: "Real strength built, with a solid base to keep progressing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition BenchPressTier3Description = new(
        Key: "BenchPressTier3Description",
        Section: "Fitness",
        Label: "Bench Press \"Advanced\" description",
        Description: "Shown under Bench Press when it's rated Advanced.",
        DefaultValue: "Well above the average lifter, built from years of consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition BenchPressTier4Description = new(
        Key: "BenchPressTier4Description",
        Section: "Fitness",
        Label: "Bench Press \"Elite\" description",
        Description: "Shown under Bench Press when it's rated Elite.",
        DefaultValue: "Among the strongest lifters, natural or not.",
        ValueKind: AppSettingValueKind.Text);

    // -- Deadlift rating (Fitness) --

    public static readonly AppSettingDefinition DeadliftTier1Max = new(
        Key: "DeadliftTier1Max",
        Section: "Fitness",
        Label: "Deadlift \"Beginner\" up to (lb)",
        Description: "Deadlift at or below this is rated Beginner.",
        DefaultValue: "250");

    public static readonly AppSettingDefinition DeadliftTier2Max = new(
        Key: "DeadliftTier2Max",
        Section: "Fitness",
        Label: "Deadlift \"Intermediate\" up to (lb)",
        Description: "Above the Beginner cutoff, up to this, is rated Intermediate.",
        DefaultValue: "375");

    public static readonly AppSettingDefinition DeadliftTier3Max = new(
        Key: "DeadliftTier3Max",
        Section: "Fitness",
        Label: "Deadlift \"Advanced\" up to (lb)",
        Description: "Above the Intermediate cutoff, up to this, is rated Advanced. Anything higher is rated Elite -- a 5-plate deadlift (495 lb: bar + 5x45 lb plates per side).",
        DefaultValue: "495");

    public static readonly AppSettingDefinition DeadliftTier1Description = new(
        Key: "DeadliftTier1Description",
        Section: "Fitness",
        Label: "Deadlift \"Beginner\" description",
        Description: "Shown under Deadlift when it's rated Beginner.",
        DefaultValue: "Just getting started -- technique and consistency matter most here.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition DeadliftTier2Description = new(
        Key: "DeadliftTier2Description",
        Section: "Fitness",
        Label: "Deadlift \"Intermediate\" description",
        Description: "Shown under Deadlift when it's rated Intermediate.",
        DefaultValue: "Real strength built, with a solid base to keep progressing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition DeadliftTier3Description = new(
        Key: "DeadliftTier3Description",
        Section: "Fitness",
        Label: "Deadlift \"Advanced\" description",
        Description: "Shown under Deadlift when it's rated Advanced.",
        DefaultValue: "Well above the average lifter, built from years of consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition DeadliftTier4Description = new(
        Key: "DeadliftTier4Description",
        Section: "Fitness",
        Label: "Deadlift \"Elite\" description",
        Description: "Shown under Deadlift when it's rated Elite.",
        DefaultValue: "Among the strongest lifters, natural or not.",
        ValueKind: AppSettingValueKind.Text);

    // -- Overhead Press rating (Fitness) --

    public static readonly AppSettingDefinition OverheadPressTier1Max = new(
        Key: "OverheadPressTier1Max",
        Section: "Fitness",
        Label: "Overhead Press \"Beginner\" up to (lb)",
        Description: "Overhead Press at or below this is rated Beginner.",
        DefaultValue: "110");

    public static readonly AppSettingDefinition OverheadPressTier2Max = new(
        Key: "OverheadPressTier2Max",
        Section: "Fitness",
        Label: "Overhead Press \"Intermediate\" up to (lb)",
        Description: "Above the Beginner cutoff, up to this, is rated Intermediate.",
        DefaultValue: "160");

    public static readonly AppSettingDefinition OverheadPressTier3Max = new(
        Key: "OverheadPressTier3Max",
        Section: "Fitness",
        Label: "Overhead Press \"Advanced\" up to (lb)",
        Description: "Above the Intermediate cutoff, up to this, is rated Advanced. Anything higher is rated Elite -- a 2-plate overhead press (225 lb: bar + 2x45 lb plates per side).",
        DefaultValue: "225");

    public static readonly AppSettingDefinition OverheadPressTier1Description = new(
        Key: "OverheadPressTier1Description",
        Section: "Fitness",
        Label: "Overhead Press \"Beginner\" description",
        Description: "Shown under Overhead Press when it's rated Beginner.",
        DefaultValue: "Just getting started -- technique and consistency matter most here.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition OverheadPressTier2Description = new(
        Key: "OverheadPressTier2Description",
        Section: "Fitness",
        Label: "Overhead Press \"Intermediate\" description",
        Description: "Shown under Overhead Press when it's rated Intermediate.",
        DefaultValue: "Real strength built, with a solid base to keep progressing.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition OverheadPressTier3Description = new(
        Key: "OverheadPressTier3Description",
        Section: "Fitness",
        Label: "Overhead Press \"Advanced\" description",
        Description: "Shown under Overhead Press when it's rated Advanced.",
        DefaultValue: "Well above the average lifter, built from years of consistent training.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly AppSettingDefinition OverheadPressTier4Description = new(
        Key: "OverheadPressTier4Description",
        Section: "Fitness",
        Label: "Overhead Press \"Elite\" description",
        Description: "Shown under Overhead Press when it's rated Elite.",
        DefaultValue: "Among the strongest lifters, natural or not.",
        ValueKind: AppSettingValueKind.Text);

    public static readonly IReadOnlyList<AppSettingDefinition> All =
    [
        CategoryStatusStrugglingMax,
        CategoryStatusNeedsAttentionMax,
        CategoryStatusOnTrackMax,
        ActiveCircleThresholdMonths,
        SocialCircleThinMax,
        SocialCircleHealthyMax,
        SocialCircleRobustMax,
        OverdueThresholdMonths,
        DateWithWifeThresholdMonths,
        VisitedMotherThresholdMonths,
        SocialTier1Description,
        SocialTier2Description,
        SocialTier3Description,
        SocialTier4Description,
        NetWorthTier1Max,
        NetWorthTier2Max,
        NetWorthTier3Max,
        NetWorthTier1Description,
        NetWorthTier2Description,
        NetWorthTier3Description,
        NetWorthTier4Description,
        CreditScoreTier1Max,
        CreditScoreTier2Max,
        CreditScoreTier3Max,
        CreditScoreTier1Description,
        CreditScoreTier2Description,
        CreditScoreTier3Description,
        CreditScoreTier4Description,
        EmergencyFundTier1Max,
        EmergencyFundTier2Max,
        EmergencyFundTier3Max,
        EmergencyFundTier1Description,
        EmergencyFundTier2Description,
        EmergencyFundTier3Description,
        EmergencyFundTier4Description,
        RetirementFundTier1Max,
        RetirementFundTier2Max,
        RetirementFundTier3Max,
        RetirementFundTier1Description,
        RetirementFundTier2Description,
        RetirementFundTier3Description,
        RetirementFundTier4Description,
        StrengthTotalTier1Max,
        StrengthTotalTier2Max,
        StrengthTotalTier3Max,
        StrengthTotalTier1Description,
        StrengthTotalTier2Description,
        StrengthTotalTier3Description,
        StrengthTotalTier4Description,
        SquatTier1Max,
        SquatTier2Max,
        SquatTier3Max,
        SquatTier1Description,
        SquatTier2Description,
        SquatTier3Description,
        SquatTier4Description,
        BenchPressTier1Max,
        BenchPressTier2Max,
        BenchPressTier3Max,
        BenchPressTier1Description,
        BenchPressTier2Description,
        BenchPressTier3Description,
        BenchPressTier4Description,
        DeadliftTier1Max,
        DeadliftTier2Max,
        DeadliftTier3Max,
        DeadliftTier1Description,
        DeadliftTier2Description,
        DeadliftTier3Description,
        DeadliftTier4Description,
        OverheadPressTier1Max,
        OverheadPressTier2Max,
        OverheadPressTier3Max,
        OverheadPressTier1Description,
        OverheadPressTier2Description,
        OverheadPressTier3Description,
        OverheadPressTier4Description,
        ArmMeasurementTier1Max,
        ArmMeasurementTier2Max,
        ArmMeasurementTier3Max,
        ArmMeasurementTier1Description,
        ArmMeasurementTier2Description,
        ArmMeasurementTier3Description,
        ArmMeasurementTier4Description,
        Vo2MaxTier1Max,
        Vo2MaxTier2Max,
        Vo2MaxTier3Max,
        Vo2MaxTier1Description,
        Vo2MaxTier2Description,
        Vo2MaxTier3Description,
        Vo2MaxTier4Description,
        WaistMeasurementTier1Max,
        WaistMeasurementTier2Max,
        WaistMeasurementTier3Max,
        WaistMeasurementTier1Description,
        WaistMeasurementTier2Description,
        WaistMeasurementTier3Description,
        WaistMeasurementTier4Description,
    ];
}
