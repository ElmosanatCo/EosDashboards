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
}
