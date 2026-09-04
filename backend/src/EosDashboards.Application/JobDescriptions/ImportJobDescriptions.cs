using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.JobDescriptions;

public sealed class ImportJobDescriptions(
    IClock clock,
    IJobDescriptionRepository repository,
    IJobDescriptionScope scope,
    IJobDescriptionImportReader importReader,
    IJobDescriptionWorkbookParser parser,
    IJobDescriptionWorkbookGenerator generator,
    IUnitOfWork unitOfWork)
{
    public async Task<IReadOnlyList<WorkbookImportResult>> ImportAsync(
        long actorUserId,
        IReadOnlyList<WorkbookImportInput> inputs,
        CancellationToken cancellationToken)
    {
        var managedDepartmentIds = await scope.GetManagedDepartmentIdsAsync(actorUserId, cancellationToken);
        var defaultDepartmentId = actorUserId > 0
            ? await importReader.FindUserDepartmentIdAsync(actorUserId, cancellationToken)
            : null;
        if (actorUserId <= 0 || defaultDepartmentId is null || !managedDepartmentIds.Contains(defaultDepartmentId.Value))
        {
            return inputs.Select(input => new WorkbookImportResult(input.FileName, false, null,
                "دسترسی مدیر برای بخش پیش‌فرض معتبر نیست.", [])).ToArray();
        }

        var results = new List<WorkbookImportResult>(inputs.Count);
        foreach (var input in inputs)
        {
            try
            {
                var workbook = await parser.ParseAsync(input.FileName, input.Content, cancellationToken);
                var departmentId = await ResolveDepartmentAsync(workbook.DepartmentName, defaultDepartmentId.Value, managedDepartmentIds, cancellationToken);
                if (departmentId is null)
                {
                    results.Add(new(workbook.FileName, false, null,
                        "بخش اعلام‌شده خارج از محدوده‌ی مجاز مدیر است یا پیدا نشد.", []));
                    continue;
                }

                var taskMatches = await importReader.FindTasksAsync(
                    departmentId.Value,
                    workbook.Tasks.Select(task => task.Title).Where(title => !string.IsNullOrWhiteSpace(title)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    cancellationToken);
                var skillMatches = await importReader.FindSkillIdsAsync(
                    departmentId.Value,
                    workbook.SkillNames.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                    cancellationToken);

                var suggestions = workbook.Tasks
                    .Where(task => !taskMatches.ContainsKey(Normalize(task.Title)))
                    .Select(task => $"وظیفه‌ی تایپی برای پیشنهاد به کاتالوگ: {task.Title.Trim()}")
                    .Concat(workbook.SkillNames
                        .Where(skill => !skillMatches.ContainsKey(Normalize(skill.Trim())))
                        .Select(skill => $"مهارت تایپی برای پیشنهاد به کاتالوگ: {skill.Trim()}"))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                var tasks = workbook.Tasks
                    .Where(task => taskMatches.ContainsKey(Normalize(task.Title)))
                    .Select(task => JobDescriptionTask.Create(
                        taskMatches[Normalize(task.Title)].Id,
                        taskMatches[Normalize(task.Title)].Title,
                        task.Description,
                        task.StartDate,
                        task.EndDate,
                        task.SortOrder))
                    .ToArray();
                var unresolvedSkillNames = workbook.SkillNames
                    .Where(skill => !skillMatches.ContainsKey(Normalize(skill)))
                    .Select(skill => skill.Trim())
                    .Where(skill => skill.Length > 0)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var unresolvedTasks = workbook.Tasks
                    .Where(task => !taskMatches.ContainsKey(Normalize(task.Title)))
                    .Select(task => new UnresolvedTaskInput(task.Title, task.Description, task.StartDate, task.EndDate, task.SortOrder))
                    .ToArray();
                var record = JobDescriptionRecord.Create(departmentId.Value, workbook.PersonName ?? string.Empty, clock.Now);
                repository.AddRecord(record);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                var version = JobDescriptionVersion.Create(
                    workbook.PersonName ?? string.Empty,
                    departmentId.Value,
                    workbook.PersonnelCode,
                    workbook.Education ?? string.Empty,
                    workbook.FieldOfStudy ?? string.Empty,
                    workbook.MinimumExperience ?? string.Empty,
                    skillMatches.Values,
                    tasks,
                    clock.Now,
                    record.Id,
                    unresolvedSkillNames,
                    unresolvedTasks);
                repository.AddVersion(version);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                version.SetExcelArtifact(generator.Generate(version, DateOnly.FromDateTime(clock.Now)), StandardWorkbookFileName(workbook.PersonName), clock.Now);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                results.Add(new(workbook.FileName, true, version.Id,
                    suggestions.Length == 0 ? "فایل با موفقیت استانداردسازی و به‌صورت پیش‌نویس ثبت شد." :
                    "فایل ثبت شد؛ برخی عناوین برای پیشنهاد به کاتالوگ شناسایی شدند.", suggestions));
            }
            catch (ArgumentException)
            {
                results.Add(new(input.FileName, false, null, "ساختار یا داده‌ی ضروری فایل قابل استفاده نیست.", []));
            }
            catch (InvalidDataException)
            {
                results.Add(new(input.FileName, false, null, "فایل اکسل معتبر یا قابل خواندن نیست.", []));
            }
        }

        return results;
    }

    private async Task<long?> ResolveDepartmentAsync(
        string? departmentName,
        long defaultDepartmentId,
        IReadOnlyCollection<long> managedDepartmentIds,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(departmentName)) return defaultDepartmentId;
        if (long.TryParse(PersianDigits(departmentName.Trim()), out var numericDepartmentId))
            return managedDepartmentIds.Contains(numericDepartmentId) ? numericDepartmentId : null;
        var resolved = await importReader.FindDepartmentIdAsync(departmentName, cancellationToken);
        return resolved is not null && managedDepartmentIds.Contains(resolved.Value) ? resolved : null;
    }

    private static string StandardWorkbookFileName(string? personName) =>
        $"شرح-وظایف-{(string.IsNullOrWhiteSpace(personName) ? "پرسنل" : personName.Trim())}.xlsx";

    private static string Normalize(string value) => value.Replace("ي", "ی").Replace("ك", "ک").Replace("‌", string.Empty).Replace(" ", string.Empty).Trim().ToLowerInvariant();
    private static string PersianDigits(string value) => value.Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4').Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
}

public sealed record WorkbookImportInput(string FileName, Stream Content);
