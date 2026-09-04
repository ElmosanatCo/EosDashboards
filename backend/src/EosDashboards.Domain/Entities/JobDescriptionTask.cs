namespace EosDashboards.Domain.Entities;

public sealed class JobDescriptionTask
{
    private JobDescriptionTask()
    {
        Title = null!;
        Description = null!;
    }

    private JobDescriptionTask(
        long taskCatalogItemId,
        string title,
        string description,
        DateOnly? startDate,
        DateOnly? endDate,
        decimal? weeklyHours,
        int sortOrder)
    {
        TaskCatalogItemId = taskCatalogItemId;
        Title = title;
        Description = description;
        StartDate = startDate;
        EndDate = endDate;
        WeeklyHours = weeklyHours;
        SortOrder = sortOrder;
    }

    public long Id { get; private set; }

    public long JobDescriptionVersionId { get; private set; }

    public long TaskCatalogItemId { get; private set; }

    public string Title { get; private set; }

    public string Description { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EndDate { get; private set; }

    public decimal? WeeklyHours { get; private set; }

    public int SortOrder { get; private set; }

    public static JobDescriptionTask Create(
        long taskCatalogItemId,
        string title,
        string description,
        DateOnly? startDate,
        DateOnly? endDate,
        int sortOrder,
        decimal? weeklyHours = null)
    {
        if (taskCatalogItemId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(taskCatalogItemId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("A task title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("A task description is required.", nameof(description));
        }

        if (sortOrder <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sortOrder));
        }

        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            throw new ArgumentException("A task end date cannot precede its start date.", nameof(endDate));
        }

        if (weeklyHours is < 0 or > 168)
        {
            throw new ArgumentOutOfRangeException(nameof(weeklyHours));
        }

        return new JobDescriptionTask(taskCatalogItemId, title.Trim(), description.Trim(), startDate, endDate, weeklyHours, sortOrder);
    }

    public bool IsActiveOn(DateOnly date) => !EndDate.HasValue || EndDate.Value >= date;
}
