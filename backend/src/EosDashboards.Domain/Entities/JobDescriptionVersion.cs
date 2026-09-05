using EosDashboards.Domain.Enums;

namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionVersion
{
    private readonly List<JobDescriptionVersionSkill> _skills = [];
    private readonly List<JobDescriptionTask> _tasks = [];
    private readonly List<JobDescriptionVersionUnresolvedSkill> _unresolvedSkills = [];
    private readonly List<JobDescriptionVersionUnresolvedTask> _unresolvedTasks = [];
    private bool _hasCatalogQualityIssues;

    private JobDescriptionVersion()
    {
        PersonName = null!;
        Education = null!;
        FieldOfStudy = null!;
        MinimumExperience = null!;
    }

    private JobDescriptionVersion(
        string personName,
        long departmentId,
        string? personnelCode,
        string education,
        string fieldOfStudy,
        string minimumExperience,
        IEnumerable<long> skillIds,
        IEnumerable<JobDescriptionTask> tasks,
        DateTime createdAt,
        long? jobDescriptionRecordId,
        IEnumerable<string> unresolvedSkillNames,
        IEnumerable<UnresolvedTaskInput> unresolvedTasks)
    {
        PersonName = personName;
        DepartmentId = departmentId;
        PersonnelCode = personnelCode;
        Education = education;
        FieldOfStudy = fieldOfStudy;
        MinimumExperience = minimumExperience;
        _skills.AddRange(skillIds.Distinct().Select(skillId => new JobDescriptionVersionSkill(0, skillId)));
        _tasks.AddRange(tasks.OrderBy(item => item.SortOrder));
        _unresolvedSkills.AddRange(unresolvedSkillNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select((name, index) => JobDescriptionVersionUnresolvedSkill.Create(name, index + 1)));
        _unresolvedTasks.AddRange(unresolvedTasks
            .OrderBy(item => item.SortOrder)
            .Select(JobDescriptionVersionUnresolvedTask.Create));
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
        WorkflowStatus = QualityStatus == JobDescriptionQualityStatus.Healthy
            ? JobDescriptionWorkflowStatus.PendingDepartmentApproval
            : JobDescriptionWorkflowStatus.PendingDataCompletion;
        JobDescriptionRecordId = jobDescriptionRecordId;
    }

    public long Id { get; private set; }

    public long? JobDescriptionRecordId { get; private set; }

    public JobDescriptionRecord? JobDescriptionRecord { get; private set; }

    public string PersonName { get; private set; }

    public long DepartmentId { get; private set; }

    public string? PersonnelCode { get; private set; }

    public string Education { get; private set; }

    public string FieldOfStudy { get; private set; }

    public string MinimumExperience { get; private set; }

    public JobDescriptionWorkflowStatus WorkflowStatus { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public DateTime? DepartmentApprovedAt { get; private set; }

    public DateTime? HumanResourcesReviewedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public byte[]? ExcelArtifact { get; private set; }

    public string? ExcelFileName { get; private set; }

    public IReadOnlyCollection<long> SkillIds => _skills.Select(item => item.SkillCatalogItemId).ToArray();

    public IReadOnlyCollection<JobDescriptionVersionSkill> Skills => _skills.AsReadOnly();

    public IReadOnlyCollection<JobDescriptionTask> Tasks => _tasks.AsReadOnly();

    public IReadOnlyCollection<JobDescriptionVersionUnresolvedSkill> UnresolvedSkills => _unresolvedSkills.AsReadOnly();

    public IReadOnlyCollection<JobDescriptionVersionUnresolvedTask> UnresolvedTasks => _unresolvedTasks.AsReadOnly();

    public JobDescriptionQualityStatus QualityStatus =>
        !_hasCatalogQualityIssues && HasCompleteProfile() && _tasks.Count > 0 &&
        _unresolvedTasks.Count == 0 &&
        _tasks.All(item => item.StartDate.HasValue && item.WeeklyHours.HasValue)
            ? JobDescriptionQualityStatus.Healthy
            : JobDescriptionQualityStatus.Incomplete;

    public bool HasCatalogQualityIssues => _hasCatalogQualityIssues;

    public static JobDescriptionVersion Create(
        string personName,
        long departmentId,
        string? personnelCode,
        string education,
        string fieldOfStudy,
        string minimumExperience,
        IEnumerable<long> skillIds,
        IEnumerable<JobDescriptionTask> tasks,
        DateTime createdAt,
        long? jobDescriptionRecordId = null,
        IEnumerable<string>? unresolvedSkillNames = null,
        IEnumerable<UnresolvedTaskInput>? unresolvedTasks = null)
    {
        ArgumentNullException.ThrowIfNull(skillIds);
        ArgumentNullException.ThrowIfNull(tasks);

        if (string.IsNullOrWhiteSpace(personName))
        {
            throw new ArgumentException("A person name is required.", nameof(personName));
        }

        if (departmentId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId));
        }

        return new JobDescriptionVersion(
            personName.Trim(),
            departmentId,
            string.IsNullOrWhiteSpace(personnelCode) ? null : personnelCode.Trim(),
            education?.Trim() ?? string.Empty,
            fieldOfStudy?.Trim() ?? string.Empty,
            minimumExperience?.Trim() ?? string.Empty,
            skillIds,
            tasks,
            createdAt,
            jobDescriptionRecordId,
            unresolvedSkillNames ?? [],
            unresolvedTasks ?? []);
    }

    public void ApproveByDepartmentManager(DateTime occurredAt)
    {
        if (QualityStatus != JobDescriptionQualityStatus.Healthy)
        {
            throw new InvalidOperationException("پرونده ناقص است و تا رفع نقص قابل ارسال برای تأیید نیست.");
        }

        if (WorkflowStatus is not (JobDescriptionWorkflowStatus.PendingDepartmentApproval or JobDescriptionWorkflowStatus.Rejected))
        {
            throw new InvalidOperationException("Only a department-pending or rejected version can be approved by the department manager.");
        }

        WorkflowStatus = JobDescriptionWorkflowStatus.UnderHumanResourcesReview;
        DepartmentApprovedAt = occurredAt;
        RejectionReason = null;
        UpdatedAt = occurredAt;
    }

    public void ApproveByHumanResources(DateTime occurredAt)
    {
        if (WorkflowStatus != JobDescriptionWorkflowStatus.UnderHumanResourcesReview)
        {
            throw new InvalidOperationException("Only a version under Human Resources review can be approved.");
        }

        if (QualityStatus != JobDescriptionQualityStatus.Healthy)
        {
            throw new InvalidOperationException("پرونده ناقص است و تا رفع نقص قابل تأیید نیست.");
        }

        WorkflowStatus = JobDescriptionWorkflowStatus.Approved;
        HumanResourcesReviewedAt = occurredAt;
        RejectionReason = null;
        UpdatedAt = occurredAt;
    }

    public void SetCatalogQualityIssues(bool hasIssues, DateTime occurredAt)
    {
        var workflowChanged = false;
        if (hasIssues && WorkflowStatus is
            JobDescriptionWorkflowStatus.PendingDepartmentApproval or
            JobDescriptionWorkflowStatus.UnderHumanResourcesReview or
            JobDescriptionWorkflowStatus.Rejected)
        {
            WorkflowStatus = JobDescriptionWorkflowStatus.PendingDataCompletion;
            DepartmentApprovedAt = null;
            HumanResourcesReviewedAt = null;
            RejectionReason = null;
            workflowChanged = true;
        }

        if (_hasCatalogQualityIssues == hasIssues && !workflowChanged)
        {
            return;
        }

        _hasCatalogQualityIssues = hasIssues;
        UpdatedAt = occurredAt;
    }

    public void RejectByHumanResources(string reason, DateTime occurredAt)
    {
        if (WorkflowStatus != JobDescriptionWorkflowStatus.UnderHumanResourcesReview)
        {
            throw new InvalidOperationException("Only a version under Human Resources review can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A rejection reason is required.", nameof(reason));
        }

        WorkflowStatus = JobDescriptionWorkflowStatus.Rejected;
        RejectionReason = reason.Trim();
        HumanResourcesReviewedAt = occurredAt;
        UpdatedAt = occurredAt;
    }

    public void Archive(DateTime occurredAt)
    {
        if (WorkflowStatus != JobDescriptionWorkflowStatus.Approved)
        {
            throw new InvalidOperationException("Only an approved version can be archived.");
        }

        WorkflowStatus = JobDescriptionWorkflowStatus.Archived;
        UpdatedAt = occurredAt;
    }

    public void SetExcelArtifact(byte[] artifact, string fileName, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        if (artifact.Length == 0) throw new ArgumentException("An Excel artifact is required.", nameof(artifact));
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("An Excel file name is required.", nameof(fileName));

        ExcelArtifact = artifact;
        ExcelFileName = fileName.Trim();
        UpdatedAt = occurredAt;
    }

    private bool HasCompleteProfile() =>
        !string.IsNullOrWhiteSpace(PersonName) &&
        !string.IsNullOrWhiteSpace(PersonnelCode) &&
        !string.IsNullOrWhiteSpace(Education) &&
        !string.IsNullOrWhiteSpace(FieldOfStudy) &&
        !string.IsNullOrWhiteSpace(MinimumExperience) &&
        _skills.Count > 0 &&
        _unresolvedSkills.Count == 0;
}
