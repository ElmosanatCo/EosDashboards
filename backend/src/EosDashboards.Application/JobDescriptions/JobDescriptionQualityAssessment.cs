namespace EosDashboards.Application.JobDescriptions;

public sealed record JobDescriptionQualityAssessment(
    IReadOnlyList<JobDescriptionQualityFinding> Findings)
{
    public bool NeedsReview => Findings.Any(item => item.Code == "missing-required-skill");

    public bool HasBlockingIssues => Findings.Any(item => item.Code != "missing-required-skill");

    public static JobDescriptionQualityAssessment From(
        IReadOnlyList<JobDescriptionQualityFinding> findings) => new(findings);
}
