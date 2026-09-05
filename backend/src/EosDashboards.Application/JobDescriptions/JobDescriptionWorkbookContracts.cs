using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.JobDescriptions;

public sealed record ImportedTask(
    string Title,
    string Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int SortOrder);

public sealed record ImportedJobDescriptionWorkbook(
    string FileName,
    string? PersonName,
    string? DepartmentName,
    string? PersonnelCode,
    string? Education,
    string? FieldOfStudy,
    string? MinimumExperience,
    IReadOnlyList<string> SkillNames,
    IReadOnlyList<ImportedTask> Tasks);

public interface IJobDescriptionWorkbookParser
{
    Task<ImportedJobDescriptionWorkbook> ParseAsync(
        string fileName,
        Stream content,
        CancellationToken cancellationToken);
}

public interface IJobDescriptionWorkbookGenerator
{
    byte[] Generate(
        JobDescriptionVersion version,
        DateOnly asOf,
        string? departmentName = null,
        IReadOnlyCollection<string>? skillNames = null);
}

public interface IJobDescriptionImportReader
{
    Task<long?> FindUserDepartmentIdAsync(long userId, CancellationToken cancellationToken);

    Task<long?> FindDepartmentIdAsync(string departmentName, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, long>> FindSkillIdsAsync(
        long departmentId,
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, TaskCatalogMatch>> FindTasksAsync(
        long departmentId,
        IReadOnlyCollection<string> titles,
        CancellationToken cancellationToken);
}

public sealed record TaskCatalogMatch(long Id, string Title);

public sealed record WorkbookImportResult(
    string FileName,
    bool Succeeded,
    long? VersionId,
    string Message,
    IReadOnlyList<string> Suggestions);
