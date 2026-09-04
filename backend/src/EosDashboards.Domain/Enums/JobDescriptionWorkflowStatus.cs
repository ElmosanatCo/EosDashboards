namespace EosDashboards.Domain.Enums;

public enum JobDescriptionWorkflowStatus
{
    PendingDepartmentApproval = 1,
    UnderHumanResourcesReview = 2,
    Approved = 3,
    Rejected = 4,
    Archived = 5,
    PendingDataCompletion = 6,
}
