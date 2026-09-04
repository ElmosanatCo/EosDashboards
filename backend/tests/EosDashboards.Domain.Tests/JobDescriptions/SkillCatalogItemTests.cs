using EosDashboards.Domain.Entities;

namespace EosDashboards.Domain.Tests.JobDescriptions;

public sealed class SkillCatalogItemTests
{
    [Fact]
    public void Public_skill_keeps_owner_department_without_becoming_department_specific()
    {
        var skill = SkillCatalogItem.CreatePublic(7, "مدیریت پروژه", new DateTime(2026, 9, 4, 12, 0, 0));

        Assert.Null(skill.DepartmentId);
        Assert.Equal(7, skill.OwnerDepartmentId);
        Assert.Equal("مدیریت پروژه", skill.Name);
        Assert.True(skill.IsActive);
    }

    [Fact]
    public void Deactivated_skill_can_be_activated_again()
    {
        var skill = SkillCatalogItem.Create(7, "مدیریت پروژه", new DateTime(2026, 9, 4, 12, 0, 0));

        skill.Deactivate(new DateTime(2026, 9, 4, 12, 1, 0));
        skill.Activate(new DateTime(2026, 9, 4, 12, 2, 0));

        Assert.True(skill.IsActive);
    }
}
