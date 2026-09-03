using EosDashboards.Domain.Entities;

namespace EosDashboards.Domain.Tests;

public sealed class DepartmentTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Unspecified);

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

    [Fact]
    public void Child_can_be_made_independent_or_moved_to_another_independent_parent()
    {
        // Break caught: preventing the approved re-parenting operations for a direct child.
        var firstParent = Department.CreateRoot("نرم افزار", Now);
        var secondParent = Department.CreateRoot("منابع انسانی", Now);
        var child = Department.CreateChild(firstParent, "فناوری اطلاعات", Now);

        child.MakeIndependent(Now.AddMinutes(1));

        Assert.Null(child.ParentDepartment);

        child.AssignParent(secondParent, Now.AddMinutes(2));

        Assert.Same(secondParent, child.ParentDepartment);
        Assert.Equal(Now.AddMinutes(2), child.UpdatedAt);
    }

    [Fact]
    public void Department_cannot_become_a_child_of_an_existing_child()
    {
        // Break caught: permitting a third hierarchy level by assigning a child as parent.
        var parent = Department.CreateRoot("نرم افزار", Now);
        var child = Department.CreateChild(parent, "فناوری اطلاعات", Now);

        Assert.Throws<InvalidOperationException>(() => parent.AssignParent(child, Now.AddMinutes(1)));
    }

    [Fact]
    public void Rename_replaces_the_department_name()
    {
        // Break caught: keeping stale department names after an approved rename.
        var department = Department.CreateRoot("نرم افزار", Now);

        department.Rename("مهندسی نرم افزار", Now.AddMinutes(1));

        Assert.Equal("مهندسی نرم افزار", department.Name);
        Assert.Equal(Now.AddMinutes(1), department.UpdatedAt);
    }
}
