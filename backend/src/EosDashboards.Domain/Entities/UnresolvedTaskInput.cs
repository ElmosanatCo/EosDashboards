namespace EosDashboards.Domain.Entities;

public sealed record UnresolvedTaskInput(
    string RawTitle,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder);
