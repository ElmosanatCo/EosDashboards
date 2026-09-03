namespace EosDashboards.ArchitectureTests;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Domain_and_application_do_not_reference_outer_layers()
    {
        Assert.DoesNotContain(
            "EosDashboards.Infrastructure",
            ReferencedBy("EosDashboards.Domain"));
        Assert.DoesNotContain(
            "EosDashboards.Api",
            ReferencedBy("EosDashboards.Domain"));
        Assert.DoesNotContain(
            "EosDashboards.Infrastructure",
            ReferencedBy("EosDashboards.Application"));
        Assert.DoesNotContain(
            "EosDashboards.Api",
            ReferencedBy("EosDashboards.Application"));
    }

    private static IReadOnlyCollection<string> ReferencedBy(string assemblyName)
    {
        return System.Reflection.Assembly
            .Load(assemblyName)
            .GetReferencedAssemblies()
            .Select(reference => reference.Name ?? string.Empty)
            .ToArray();
    }
}
