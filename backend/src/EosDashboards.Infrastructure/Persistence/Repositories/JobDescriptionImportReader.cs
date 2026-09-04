using EosDashboards.Application.JobDescriptions;
using Microsoft.EntityFrameworkCore;

namespace EosDashboards.Infrastructure.Persistence.Repositories;

public sealed class JobDescriptionImportReader(EosDashboardDbContext context) : IJobDescriptionImportReader
{
    public Task<long?> FindUserDepartmentIdAsync(long userId, CancellationToken cancellationToken) =>
        context.Users.AsNoTracking().Where(user => user.Id == userId && user.IsActive).Select(user => (long?)user.DepartmentId).SingleOrDefaultAsync(cancellationToken);

    public Task<long?> FindDepartmentIdAsync(string departmentName, CancellationToken cancellationToken) =>
        context.Departments.AsNoTracking().Where(department => department.Name == departmentName.Trim()).Select(department => (long?)department.Id).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<string, long>> FindSkillIdsAsync(long departmentId, IReadOnlyCollection<string> names, CancellationToken cancellationToken)
    {
        var items = await context.SkillCatalogItems.AsNoTracking().Where(skill => skill.IsActive && (skill.DepartmentId == null || skill.DepartmentId == departmentId)).ToListAsync(cancellationToken);
        return items.Where(item => names.Any(name => Normalize(name) == Normalize(item.Name))).ToDictionary(item => Normalize(item.Name), item => item.Id, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyDictionary<string, TaskCatalogMatch>> FindTasksAsync(long departmentId, IReadOnlyCollection<string> titles, CancellationToken cancellationToken)
    {
        var items = await context.TaskCatalogItems.AsNoTracking().Where(task => task.IsActive && task.DepartmentId == departmentId).ToListAsync(cancellationToken);
        return items.Where(item => titles.Any(title => Normalize(title) == Normalize(item.Title))).ToDictionary(item => Normalize(item.Title), item => new TaskCatalogMatch(item.Id, item.Title), StringComparer.OrdinalIgnoreCase);
    }

    private static string Normalize(string value) => value.Replace("ي", "ی").Replace("ك", "ک").Replace("‌", string.Empty).Replace(" ", string.Empty).Trim().ToLowerInvariant();
}
