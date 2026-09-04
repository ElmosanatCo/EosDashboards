using System.IdentityModel.Tokens.Jwt;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Security;
using EosDashboards.Application.JobDescriptions;
using EosDashboards.Domain.Enums;

namespace EosDashboards.Api.JobDescriptions;

public static class JobDescriptionEndpoints
{
    public static IEndpointRouteBuilder MapJobDescriptionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/job-descriptions")
            .WithTags("Job descriptions")
            .RequireAuthorization("ActiveUser");
        group.MapGet("", ListAsync);
        group.MapGet("/dashboard", DashboardAsync);
        group.MapGet("/catalog", CatalogAsync);
        group.MapGet("/managed-departments", ManagedDepartmentsAsync);
        group.MapGet("/human-resources-review", HumanResourcesReviewListAsync);
        group.MapGet("/human-resources-catalog", HumanResourcesCatalogAsync);
        group.MapGet("/{versionId:long}", DetailAsync);
        group.MapGet("/{versionId:long}/analysis", AnalysisAsync);
        group.MapGet("/{versionId:long}/excel", DownloadExcelAsync);
        group.MapPost("/catalog/skills", CreateSkillAsync);
        group.MapPost("/catalog/public-skills", CreatePublicSkillAsync);
        group.MapPost("/catalog/tasks", CreateTaskAsync);
        group.MapPut("/catalog/tasks/{taskId:long}/required-skills", SetRequiredSkillsAsync);
        group.MapPut("/catalog/public-skills/{skillId:long}", RenamePublicSkillAsync);
        group.MapDelete("/catalog/public-skills/{skillId:long}", DeactivatePublicSkillAsync);
        group.MapPut("/catalog/skills/{skillId:long}", RenameDepartmentSkillAsync);
        group.MapDelete("/catalog/skills/{skillId:long}", DeactivateDepartmentSkillAsync);
        group.MapPut("/catalog/tasks/{taskId:long}", RenameDepartmentTaskAsync);
        group.MapDelete("/catalog/tasks/{taskId:long}", DeactivateDepartmentTaskAsync);
        group.MapPost("", CreateAsync);
        group.MapPost("/import", ImportAsync);
        group.MapPut("/{versionId:long}", ReviseAsync);
        group.MapPost("/{versionId:long}/department-approval", ApproveByDepartmentManagerAsync);
        group.MapPost("/{versionId:long}/human-resources-approval", ApproveByHumanResourcesAsync);
        group.MapPost("/{versionId:long}/human-resources-rejection", RejectByHumanResourcesAsync);
        group.MapPost("/{versionId:long}/archive", ArchiveAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext context,
        ManageJobDescriptions manager,
        IJobDescriptionScope scope,
        long? departmentId = null,
        CancellationToken token = default)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var managedDepartments = await scope.GetManagedDepartmentIdsAsync(actor, token);
        var items = await manager.ListAsync(actor, managedDepartments, departmentId, token);
        return items is null
            ? Problem(context, 403, "department_scope_forbidden", "The selected department is outside your management scope.")
            : Results.Ok(items.Select(item => new JobDescriptionListResponse(
                item.Id, item.DepartmentId, item.PersonName,
                Workflow(item.WorkflowStatus), Quality(item.QualityStatus), item.UpdatedAt)).ToArray());
    }

    private static async Task<IResult> DashboardAsync(
        HttpContext context,
        GetDepartmentDashboard dashboard,
        long? departmentId = null,
        CancellationToken token = default)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var metrics = await dashboard.HandleAsync(actor, departmentId, token);
        return metrics is null
            ? Problem(context, 403, "department_scope_forbidden", "The selected department is outside your management scope.")
            : Results.Ok(metrics);
    }

    private static async Task<IResult> CatalogAsync(
        HttpContext context,
        ManageJobDescriptions manager,
        IJobDescriptionScope scope,
        long? departmentId = null,
        CancellationToken token = default)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var managedDepartments = await scope.GetManagedDepartmentIdsAsync(actor, token);
        var catalog = await manager.ListCatalogAsync(actor, managedDepartments, departmentId, token);
        if (catalog is null)
            return Problem(context, 403, "department_scope_forbidden", "The selected department is outside your management scope.");

        var skills = catalog.Value.Skills.Select(skill => new SkillCatalogResponse(
            skill.Id,
            skill.DepartmentId,
            skill.Name,
            skill.OwnerDepartmentId,
            skill.UsageDepartmentIds.Count,
            CanManagerEditSkill(skill, managedDepartments),
            CanManagerEditSkill(skill, managedDepartments))).ToArray();
        return Results.Ok(new JobDescriptionCatalogResponse(
            skills,
            catalog.Value.Tasks.Select(task => new TaskCatalogResponse(task.Id, task.DepartmentId, task.Title, task.IsProject, task.RequiredSkillIds)).ToArray()));
    }

    private static async Task<IResult> ManagedDepartmentsAsync(
        HttpContext context,
        IJobDescriptionScope scope,
        IJobDescriptionDepartmentReader departments,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var ids = await scope.GetManagedDepartmentIdsAsync(actor, token);
        if (ids.Count == 0) return Problem(context, 403, "department_scope_forbidden", "No managed department is available.");
        return Results.Ok(await departments.ListAsync(ids[0], ids, token));
    }

    private static async Task<IResult> HumanResourcesReviewListAsync(HttpContext context, ManageJobDescriptions manager, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var items = await manager.ListForHumanResourcesAsync(actor, token);
        return items is null
            ? Problem(context, 403, "human_resources_forbidden", "Human Resources review access is required.")
            : Results.Ok(items.Select(item => new JobDescriptionListResponse(
                item.Id, item.DepartmentId, item.PersonName,
                Workflow(item.WorkflowStatus), Quality(item.QualityStatus), item.UpdatedAt)).ToArray());
    }

    private static async Task<IResult> HumanResourcesCatalogAsync(HttpContext context, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var skills = await catalog.ListPublicSkillsAsync(actor, token);
        return skills is null
            ? Problem(context, 403, "human_resources_forbidden", "Human Resources catalog access is required.")
            : Results.Ok(skills.Select(skill => new SkillCatalogResponse(
                skill.Id, skill.DepartmentId, skill.Name, skill.OwnerDepartmentId,
                skill.UsageDepartmentIds.Count, true, true)).ToArray());
    }

    private static async Task<IResult> DetailAsync(long versionId, HttpContext context, ManageJobDescriptions manager, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var version = await manager.GetForAuthorizedReadAsync(actor, versionId, token);
        return version is null
            ? Problem(context, 404, "job_description_not_found", "The requested job description was not found.")
            : Results.Ok(new JobDescriptionDetailResponse(
                version.Id, version.DepartmentId, version.PersonName, version.PersonnelCode,
                version.Education, version.FieldOfStudy, version.MinimumExperience,
                version.SkillIds,
                version.Tasks.Select(task => new JobDescriptionTaskResponse(
                    task.TaskCatalogItemId, task.Title, task.Description, task.StartDate, task.EndDate, task.SortOrder, task.WeeklyHours)).ToArray(),
                version.UnresolvedSkills.Select(skill => new JobDescriptionUnresolvedSkillResponse(skill.RawName, skill.SortOrder)).ToArray(),
                version.UnresolvedTasks.Select(task => new JobDescriptionUnresolvedTaskResponse(
                    task.RawTitle, task.Description, task.StartDate, task.EndDate, task.SortOrder)).ToArray(),
                Workflow(version.WorkflowStatus), Quality(version.QualityStatus), version.RejectionReason));
    }

    private static async Task<IResult> AnalysisAsync(long versionId, HttpContext context, AnalyzeJobDescription analyzer, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var findings = await analyzer.AnalyzeAsync(actor, versionId, token);
        return findings is null
            ? Problem(context, 404, "job_description_not_found", "The requested job description was not found or is outside your scope.")
            : Results.Ok(findings);
    }

    private static async Task<IResult> CreateSkillAsync(HttpContext context, CreateSkillRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await catalog.CreateSkillAsync(actor, new CreateSkillCommand(request.DepartmentId, request.Name), token);
        return CatalogResult(context, result);
    }

    private static async Task<IResult> CreatePublicSkillAsync(HttpContext context, CreatePublicSkillRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await catalog.CreatePublicSkillAsync(actor, new CreatePublicSkillCommand(request.OwnerDepartmentId, request.Name), token);
        return CatalogResult(context, result);
    }

    private static async Task<IResult> CreateTaskAsync(HttpContext context, CreateTaskRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await catalog.CreateTaskAsync(actor, new CreateTaskCommand(request.DepartmentId, request.Title, request.IsProject), token);
        return CatalogResult(context, result);
    }

    private static async Task<IResult> SetRequiredSkillsAsync(long taskId, HttpContext context, SetTaskRequiredSkillsRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await catalog.SetRequiredSkillsAsync(actor, new SetTaskRequiredSkillsCommand(taskId, request.SkillIds), token);
        return CatalogResult(context, result);
    }

    private static async Task<IResult> RenamePublicSkillAsync(long skillId, HttpContext context, UpdatePublicSkillRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.RenamePublicSkillAsync(actor, skillId, request.Name, token));
    }

    private static async Task<IResult> DeactivatePublicSkillAsync(long skillId, HttpContext context, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.DeactivatePublicSkillAsync(actor, skillId, token));
    }

    private static async Task<IResult> RenameDepartmentSkillAsync(long skillId, HttpContext context, UpdateCatalogNameRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.RenameDepartmentSkillAsync(actor, skillId, request.Name, token));
    }

    private static async Task<IResult> DeactivateDepartmentSkillAsync(long skillId, HttpContext context, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.DeactivateDepartmentSkillAsync(actor, skillId, token));
    }

    private static async Task<IResult> RenameDepartmentTaskAsync(long taskId, HttpContext context, UpdateCatalogNameRequest request, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.RenameDepartmentTaskAsync(actor, taskId, request.Name, token));
    }

    private static async Task<IResult> DeactivateDepartmentTaskAsync(long taskId, HttpContext context, ManageCatalog catalog, CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return CatalogResult(context, await catalog.DeactivateDepartmentTaskAsync(actor, taskId, token));
    }

    private static IResult CatalogResult(HttpContext context, CatalogOperationResult result) => result.Status switch
    {
        CatalogOperationStatus.Succeeded => Results.Ok(new { id = result.Id }),
        CatalogOperationStatus.NotFound => Problem(context, 404, "catalog_item_not_found", "The catalog item was not found."),
        CatalogOperationStatus.Forbidden => Problem(context, 403, "catalog_scope_forbidden", "You are not authorized for this department catalog."),
        CatalogOperationStatus.Conflict => Problem(context, 409, "catalog_conflict", "The catalog item has changed."),
        _ => Problem(context, 400, "invalid_catalog_request", "The catalog request is invalid."),
    };

    private static bool CanManagerEditSkill(SkillCatalogListItem skill, IReadOnlyCollection<long> managedDepartmentIds) =>
        skill.DepartmentId is { } departmentId
            ? managedDepartmentIds.Contains(departmentId)
            : skill.OwnerDepartmentId is { } ownerDepartmentId &&
              managedDepartmentIds.Contains(ownerDepartmentId) &&
              skill.UsageDepartmentIds.All(departmentId => departmentId == ownerDepartmentId);

    private static async Task<IResult> CreateAsync(
        HttpContext context,
        CreateJobDescriptionRequest request,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await manager.CreateAsync(actor, new CreateJobDescriptionCommand(
            request.PersonName,
            request.DepartmentId,
            request.PersonnelCode,
            request.Education,
            request.FieldOfStudy,
            request.MinimumExperience,
            request.SkillIds,
            request.Tasks.Select(task => new JobDescriptionTaskInput(
                task.TaskCatalogItemId, task.Title, task.Description,
                task.StartDate, task.EndDate, task.SortOrder, task.WeeklyHours)).ToArray()), token);
        return Operation(context, result, created: true);
    }

    private static async Task<IResult> ImportAsync(
        HttpContext context,
        ImportJobDescriptions importer,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        if (!context.Request.HasFormContentType) return Problem(context, 400, "invalid_workbook_upload", "A multipart Excel upload is required.");
        var form = await context.Request.ReadFormAsync(token);
        if (form.Files.Count == 0) return Problem(context, 400, "invalid_workbook_upload", "At least one Excel file is required.");
        var inputs = form.Files.Select(file => new WorkbookImportInput(file.FileName, file.OpenReadStream())).ToArray();
        try
        {
            return Results.Ok(await importer.ImportAsync(actor, inputs, token));
        }
        finally
        {
            foreach (var input in inputs) await input.Content.DisposeAsync();
        }
    }

    private static async Task<IResult> DownloadExcelAsync(
        long versionId,
        HttpContext context,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var version = await manager.GetForAuthorizedReadAsync(actor, versionId, token);
        if (version?.ExcelArtifact is null) return Problem(context, 404, "excel_artifact_not_found", "The standard Excel artifact is not available.");
        return Results.File(version.ExcelArtifact, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", version.ExcelFileName ?? $"job-description-{version.Id}.xlsx");
    }

    private static async Task<IResult> ReviseAsync(
        long versionId,
        HttpContext context,
        CreateJobDescriptionRequest request,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        var result = await manager.ReviseAsync(actor, versionId, new CreateJobDescriptionCommand(
            request.PersonName, request.DepartmentId, request.PersonnelCode,
            request.Education, request.FieldOfStudy, request.MinimumExperience,
            request.SkillIds,
            request.Tasks.Select(task => new JobDescriptionTaskInput(
                task.TaskCatalogItemId, task.Title, task.Description,
                task.StartDate, task.EndDate, task.SortOrder, task.WeeklyHours)).ToArray()), token);
        return Operation(context, result, false);
    }

    private static async Task<IResult> ApproveByDepartmentManagerAsync(
        long versionId,
        HttpContext context,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return Operation(context, await manager.ApproveByDepartmentManagerAsync(actor, versionId, token), false);
    }

    private static async Task<IResult> ApproveByHumanResourcesAsync(
        long versionId,
        HttpContext context,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return Operation(context, await manager.ApproveByHumanResourcesAsync(actor, versionId, token), false);
    }

    private static async Task<IResult> RejectByHumanResourcesAsync(
        long versionId,
        HttpContext context,
        RejectJobDescriptionRequest request,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return Operation(context, await manager.RejectByHumanResourcesAsync(actor, versionId, request.Reason, token), false);
    }

    private static async Task<IResult> ArchiveAsync(
        long versionId,
        HttpContext context,
        ManageJobDescriptions manager,
        CancellationToken token)
    {
        if (!TryActor(context, out var actor)) return Unauthorized(context);
        return Operation(context, await manager.ArchiveAsync(actor, versionId, token), false);
    }

    private static IResult Operation(HttpContext context, JobDescriptionOperationResult result, bool created) => result.Status switch
    {
        JobDescriptionOperationStatus.Succeeded => created
            ? Results.Created($"/api/v1/job-descriptions/{result.Version!.Id}", Version(result.Version))
            : Results.Ok(Version(result.Version!)),
        JobDescriptionOperationStatus.NotFound => Problem(context, 404, "job_description_not_found", "The requested job description was not found."),
        JobDescriptionOperationStatus.Forbidden => Problem(context, 403, "job_description_forbidden", "You are not authorized for this job description operation."),
        JobDescriptionOperationStatus.Incomplete => Problem(context, 409, "incomplete_job_description", "شرح وظیفه ناقص است؛ ابتدا موارد تطبیق‌نشده و داده‌های ناقص را برطرف کنید."),
        JobDescriptionOperationStatus.Conflict => Problem(context, 409, "job_description_conflict", "The job description is not in a state that permits this operation."),
        _ => Problem(context, 400, "invalid_job_description_request", "The job description request is invalid."),
    };

    private static JobDescriptionOperationResponse Version(EosDashboards.Domain.Entities.JobDescriptionVersion version) =>
        new(version.Id, Workflow(version.WorkflowStatus), Quality(version.QualityStatus), version.RejectionReason);

    private static string Workflow(EosDashboards.Domain.Enums.JobDescriptionWorkflowStatus status) => status switch
    {
        JobDescriptionWorkflowStatus.PendingDepartmentApproval => "منتظر تأیید",
        JobDescriptionWorkflowStatus.UnderHumanResourcesReview => "در حال بررسی",
        JobDescriptionWorkflowStatus.Approved => "تأیید شده",
        JobDescriptionWorkflowStatus.Rejected => "رد شده",
        JobDescriptionWorkflowStatus.Archived => "آرشیو شده",
        JobDescriptionWorkflowStatus.PendingDataCompletion => "منتظر رفع نقص",
        _ => "نامشخص",
    };

    private static string Quality(EosDashboards.Domain.Enums.JobDescriptionQualityStatus status) =>
        status == JobDescriptionQualityStatus.Healthy ? "سالم" : "ناقص";

    private static bool TryActor(HttpContext context, out long actor) => SessionAuthorizationHandler.TryReadId(context.User, JwtRegisteredClaimNames.Sub, out actor);
    private static IResult Unauthorized(HttpContext context) => Problem(context, 401, "invalid_access_token", "Authentication is required.");
    private static IResult Problem(HttpContext context, int status, string code, string detail) => ApiResults.Problem(context, status, code, detail);
}
