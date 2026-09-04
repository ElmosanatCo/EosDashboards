namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionVersionUnresolvedTask
{
    private JobDescriptionVersionUnresolvedTask()
    {
        RawTitle = null!;
        Description = null!;
    }

    private JobDescriptionVersionUnresolvedTask(
        string rawTitle,
        string description,
        DateOnly? startDate,
        DateOnly? endDate,
        int sortOrder)
    {
        RawTitle = rawTitle;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        SortOrder = sortOrder;
    }

    public long Id { get; private set; }

    public long JobDescriptionVersionId { get; private set; }

    public string RawTitle { get; private set; }

    public string Description { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public int SortOrder { get; private set; }

    public static JobDescriptionVersionUnresolvedTask Create(UnresolvedTaskInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.RawTitle))
        {
            throw new ArgumentException("An unresolved task title is required.", nameof(input));
        }

        if (string.IsNullOrWhiteSpace(input.Description))
        {
            throw new ArgumentException("An unresolved task description is required.", nameof(input));
        }

        if (input.SortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        if (input.StartDate.HasValue && input.EndDate.HasValue && input.EndDate.Value < input.StartDate.Value)
        {
            throw new ArgumentException("A task end date cannot precede its start date.", nameof(input));
        }

        return new JobDescriptionVersionUnresolvedTask(
            input.RawTitle.Trim(),
            input.Description.Trim(),
            input.StartDate,
            input.EndDate,
            input.SortOrder);
    }
}
