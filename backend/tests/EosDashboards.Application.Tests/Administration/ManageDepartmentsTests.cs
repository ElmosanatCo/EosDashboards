using System.Reflection;
using EosDashboards.Application.Administration;
using EosDashboards.Application.Tests.Auth;
using EosDashboards.Domain.Entities;

namespace EosDashboards.Application.Tests.Administration;

public sealed class ManageDepartmentsTests
{
    private static readonly DateTime Now = new(2026, 9, 3, 10, 0, 0, DateTimeKind.Unspecified);

    [Fact]
    public async Task Create_child_rejects_a_parent_that_is_already_a_child()
    {
        var context = new ManageDepartmentsContext();
        var child = context.AddDepartment("زیرمجموعه", context.Root);

        var result = await context.UseCase.CreateAsync(
            11,
            new CreateDepartmentCommand("سطح سوم", child.Id),
            CancellationToken.None);

        Assert.Equal(DepartmentOperationStatus.Invalid, result.Status);
        Assert.Empty(context.Audit.Records);
    }

    [Fact]
    public async Task Delete_rejects_a_department_with_users_or_children()
    {
        var context = new ManageDepartmentsContext();
        context.Departments.AssignedUserCounts[context.Root.Id] = 1;

        var result = await context.UseCase.DeleteAsync(
            11,
            new DeleteDepartmentCommand(context.Root.Id, context.Root.RowVersion),
            CancellationToken.None);

        Assert.Equal(DepartmentOperationStatus.NotEmpty, result.Status);
        Assert.Contains(context.Root, context.Departments.Departments);
        Assert.Empty(context.Audit.Records);
    }

    [Fact]
    public async Task Update_to_a_child_is_rejected_when_the_department_has_children()
    {
        var context = new ManageDepartmentsContext();
        context.Departments.ChildCounts[context.Root.Id] = 1;

        var result = await context.UseCase.UpdateAsync(
            11,
            new UpdateDepartmentCommand(context.Root.Id, "ریشه", context.AlternateRoot.Id, context.Root.RowVersion),
            CancellationToken.None);

        Assert.Equal(DepartmentOperationStatus.Invalid, result.Status);
        Assert.Empty(context.Audit.Records);
    }

    [Fact]
    public async Task Create_root_writes_an_audit_record()
    {
        var context = new ManageDepartmentsContext();

        var result = await context.UseCase.CreateAsync(
            11,
            new CreateDepartmentCommand("واحد جدید", null),
            CancellationToken.None);

        Assert.Equal(DepartmentOperationStatus.Succeeded, result.Status);
        Assert.Equal("DepartmentCreated", Assert.Single(context.Audit.Records).EventCode);
    }

    private sealed class ManageDepartmentsContext
    {
        public ManageDepartmentsContext()
        {
            Root = AddDepartment("ریشه", null);
            AlternateRoot = AddDepartment("ریشه دوم", null);
            UnitOfWork = new FakeUnitOfWork(Challenges, Sessions);
            UseCase = new ManageDepartments(Clock, Correlation, Departments, Audit, UnitOfWork);
        }

        public FakeClock Clock { get; } = new(Now);
        public FakeCorrelationContext Correlation { get; } = new("trace-test");
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeAuditWriter Audit { get; } = new();
        public FakeOtpChallengeRepository Challenges { get; } = new();
        public FakeUserSessionRepository Sessions { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; }
        public ManageDepartments UseCase { get; }
        public Department Root { get; }
        public Department AlternateRoot { get; }

        public Department AddDepartment(string name, Department? parent)
        {
            var department = parent is null
                ? Department.CreateRoot(name, Now)
                : Department.CreateChild(parent, name, Now);
            EntityId.Set(department, Departments.Departments.Count + 1);
            typeof(Department).GetProperty(nameof(Department.RowVersion), BindingFlags.Instance | BindingFlags.Public)!
                .SetValue(department, new byte[] { 1, 2, 3, 4 });
            Departments.Departments.Add(department);
            return department;
        }
    }
}
