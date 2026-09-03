using EosDashboards.Application.Abstractions;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Administration;

public enum DepartmentOperationStatus
{
    Succeeded,
    NotFound,
    Invalid,
    DuplicateName,
    Conflict,
    NotEmpty,
}

public sealed record DepartmentOperationResult(DepartmentOperationStatus Status, Department? Department)
{
    public override string ToString() => nameof(DepartmentOperationResult);
}

public sealed record CreateDepartmentCommand(string Name, long? ParentDepartmentId)
{
    public override string ToString() => nameof(CreateDepartmentCommand);
}

public sealed record UpdateDepartmentCommand(long DepartmentId, string Name, long? ParentDepartmentId, byte[] ExpectedRowVersion)
{
    public override string ToString() => nameof(UpdateDepartmentCommand);
}

public sealed record DeleteDepartmentCommand(long DepartmentId, byte[] ExpectedRowVersion)
{
    public override string ToString() => nameof(DeleteDepartmentCommand);
}

public sealed class ManageDepartments(
    IClock clock,
    ICorrelationContext correlationContext,
    IDepartmentRepository departments,
    IAuditWriter auditWriter,
    IUnitOfWork unitOfWork)
{
    private const string OperationKey = "ManageDepartments";

    public async Task<DepartmentOperationResult> CreateAsync(
        long actorUserId,
        CreateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || command is null || string.IsNullOrWhiteSpace(command.Name) || command.ParentDepartmentId <= 0)
        {
            return Invalid();
        }

        var name = command.Name.Trim();
        DepartmentOperationResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(OperationKey, async token =>
        {
            if (await departments.FindByNameAsync(name, token) is not null)
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.DuplicateName, null);
                return;
            }

            Department? parent = null;
            if (command.ParentDepartmentId is { } parentDepartmentId)
            {
                parent = await departments.GetForUpdateAsync(parentDepartmentId, token);
                if (parent is null || parent.ParentDepartmentId is not null || parent.ParentDepartment is not null)
                {
                    result = Invalid();
                    return;
                }
            }

            var department = parent is null
                ? Department.CreateRoot(name, clock.Now)
                : Department.CreateChild(parent, name, clock.Now);
            departments.Add(department);
            await unitOfWork.SaveChangesAsync(token);
            await WriteAuditAsync(actorUserId, department.Id, "DepartmentCreated", token);
            await unitOfWork.SaveChangesAsync(token);
            result = new DepartmentOperationResult(DepartmentOperationStatus.Succeeded, department);
        }, cancellationToken);
        return result;
    }

    public async Task<DepartmentOperationResult> UpdateAsync(
        long actorUserId,
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || command is null || command.DepartmentId <= 0 ||
            string.IsNullOrWhiteSpace(command.Name) || !HasExpectedRowVersion(command.ExpectedRowVersion) ||
            command.ParentDepartmentId <= 0)
        {
            return Invalid();
        }

        var name = command.Name.Trim();
        DepartmentOperationResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(OperationKey, async token =>
        {
            var department = await departments.GetForUpdateAsync(command.DepartmentId, token);
            if (department is null)
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.NotFound, null);
                return;
            }

            if (!department.RowVersion.SequenceEqual(command.ExpectedRowVersion))
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.Conflict, null);
                return;
            }

            var duplicate = await departments.FindByNameAsync(name, token);
            if (duplicate is not null && duplicate.Id != department.Id)
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.DuplicateName, null);
                return;
            }

            Department? parent = null;
            if (command.ParentDepartmentId is { } parentDepartmentId)
            {
                if (parentDepartmentId == department.Id || await departments.CountChildrenAsync(department.Id, token) > 0)
                {
                    result = Invalid();
                    return;
                }

                parent = await departments.GetForUpdateAsync(parentDepartmentId, token);
                if (parent is null || parent.ParentDepartmentId is not null || parent.ParentDepartment is not null)
                {
                    result = Invalid();
                    return;
                }
            }

            var now = clock.Now;
            department.Rename(name, now);
            if (parent is null)
            {
                department.MakeIndependent(now);
            }
            else
            {
                department.AssignParent(parent, now);
            }

            await WriteAuditAsync(actorUserId, department.Id, "DepartmentUpdated", token);
            await unitOfWork.SaveChangesAsync(token);
            result = new DepartmentOperationResult(DepartmentOperationStatus.Succeeded, department);
        }, cancellationToken);
        return result;
    }

    public async Task<DepartmentOperationResult> DeleteAsync(
        long actorUserId,
        DeleteDepartmentCommand command,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || command is null || command.DepartmentId <= 0 || !HasExpectedRowVersion(command.ExpectedRowVersion))
        {
            return Invalid();
        }

        DepartmentOperationResult result = Invalid();
        await unitOfWork.ExecuteSerializedTransactionAsync(OperationKey, async token =>
        {
            var department = await departments.GetForUpdateAsync(command.DepartmentId, token);
            if (department is null)
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.NotFound, null);
                return;
            }

            if (!department.RowVersion.SequenceEqual(command.ExpectedRowVersion))
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.Conflict, null);
                return;
            }

            if (await departments.CountChildrenAsync(department.Id, token) > 0 ||
                await departments.CountAssignedUsersAsync(department.Id, token) > 0)
            {
                result = new DepartmentOperationResult(DepartmentOperationStatus.NotEmpty, null);
                return;
            }

            departments.Remove(department);
            await WriteAuditAsync(actorUserId, department.Id, "DepartmentDeleted", token);
            await unitOfWork.SaveChangesAsync(token);
            result = new DepartmentOperationResult(DepartmentOperationStatus.Succeeded, department);
        }, cancellationToken);
        return result;
    }

    private Task WriteAuditAsync(long actorUserId, long subjectId, string eventCode, CancellationToken cancellationToken) =>
        auditWriter.WriteAsync(new AuditRecord(actorUserId, null, eventCode, true, correlationContext.TraceId,
            new Dictionary<string, string> { ["departmentId"] = subjectId.ToString(System.Globalization.CultureInfo.InvariantCulture) }), cancellationToken);

    private static bool HasExpectedRowVersion(byte[]? rowVersion) => rowVersion is { Length: > 0 };

    private static DepartmentOperationResult Invalid() => new(DepartmentOperationStatus.Invalid, null);
}
