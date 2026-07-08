using FluentAssertions;
using HairyPaws.Application.Common.CQRS;
using HairyPaws.Application.Common.Ports;
using HairyPaws.Contracts.Common.Responses;
using HairyPaws.Domain.Pets.Entities;
using HairyPaws.Infrastructure.Persistence;

namespace HairyPaws.Tests.Unit.Architecture;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_ShouldRemainIndependentFromOuterLayers()
    {
        var references = GetReferencedAssemblyNames(typeof(Pet));

        references.Should().NotContain("HairyPaws.Application");
        references.Should().NotContain("HairyPaws.Contracts");
        references.Should().NotContain("HairyPaws.Infrastructure");
        references.Should().NotContain("HairyPaws.Api");
    }

    [Fact]
    public void Application_ShouldNotDependOnAdaptersOrPresentation()
    {
        var references = GetReferencedAssemblyNames(typeof(ICommandHandler<,>));

        references.Should().Contain("HairyPaws.Domain");
        references.Should().Contain("HairyPaws.Contracts");
        references.Should().NotContain("HairyPaws.Infrastructure");
        references.Should().NotContain("HairyPaws.Api");
    }

    [Fact]
    public void Infrastructure_ShouldImplementApplicationPortsFromTheOutside()
    {
        var references = GetReferencedAssemblyNames(typeof(ApplicationDbContext));

        references.Should().Contain("HairyPaws.Application");
        references.Should().Contain("HairyPaws.Domain");
    }

    [Fact]
    public void Api_ShouldBeTheCompositionRoot()
    {
        var references = GetReferencedAssemblyNames(typeof(Program));

        references.Should().Contain("HairyPaws.Application");
        references.Should().Contain("HairyPaws.Contracts");
        references.Should().Contain("HairyPaws.Infrastructure");
    }

    [Fact]
    public void ApplicationPorts_ShouldBeExplicitlyNamedAsPorts()
    {
        typeof(IApplicationDbContext).Namespace.Should().Be("HairyPaws.Application.Common.Ports");
        typeof(IJwtTokenService).Namespace.Should().Be("HairyPaws.Application.Common.Ports");
        typeof(IPasswordHasher).Namespace.Should().Be("HairyPaws.Application.Common.Ports");
        typeof(PagedResponse<>).Namespace.Should().Be("HairyPaws.Contracts.Common.Responses");
    }

    private static IReadOnlyCollection<string> GetReferencedAssemblyNames(Type type)
    {
        return type.Assembly.GetReferencedAssemblies()
            .Select(assembly => assembly.Name)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToArray();
    }
}
