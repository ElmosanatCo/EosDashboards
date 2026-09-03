using EosDashboards.Domain.Entities;

namespace EosDashboards.Domain.Tests;

public sealed class DepartmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_root_has_no_parent()
    {
        var department = Department.CreateRoot("نرم افزار", Now);

        Assert.Equal("نرم افزار", department.Name);
        Assert.Null(department.ParentDepartmentId);
    }

    [Fact]
    public void Create_child_links_to_an_independent_parent()
    {
        var parent = Department.CreateRoot("نرم افزار", Now);

        var child = Department.CreateChild(parent, "فناوری اطلاعات", Now);

        Assert.Same(parent, child.ParentDepartment);
        Assert.Null(child.ParentDepartmentId);
    }

    [Fact]
    public void Create_child_rejects_a_parent_that_is_already_a_child()
    {
        var parent = Department.CreateRoot("نرم افزار", Now);
        var child = Department.CreateChild(parent, "فناوری اطلاعات", Now);

        Assert.Throws<InvalidOperationException>(() =>
            Department.CreateChild(child, "زیرساخت", Now));
    }
}
