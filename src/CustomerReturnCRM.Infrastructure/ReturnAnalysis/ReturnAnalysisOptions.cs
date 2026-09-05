namespace CustomerReturnCRM.Infrastructure.ReturnAnalysis;

public sealed class ReturnAnalysisOptions
{
    public const string SectionName = "ReturnAnalysis";

    public int DueSoonDays { get; set; } = 7;
    public int AtRiskDays { get; set; } = 30;
    public int NoRecentVisitDays { get; set; } = 90;
}
