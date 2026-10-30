using Families.Domain;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Families.Application.Tests;

/// <summary>Runs handlers through the real MediatR pipeline so the behaviours are exercised together with the handler.</summary>
internal sealed class FamiliesHarness
{
    public FamiliesHarness()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(Families.Object);
        services.AddFamiliesApplication();

        Services = services.BuildServiceProvider();
        Mediator = Services.GetRequiredService<IMediator>();
    }

    public Mock<IFamilyRepository> Families { get; } = new();

    public ServiceProvider Services { get; }

    public IMediator Mediator { get; }

    public Family GivenFamily(string name = "Jansen", params string[] members)
    {
        Family family = Family.Create(FamilyName.Create(name));

        foreach (string member in members)
        {
            family.AddMember(EmailAddress.Create(member));
            Families.Setup(repository => repository.FindByMemberAsync(EmailAddress.Create(member), It.IsAny<CancellationToken>())).ReturnsAsync(family);
        }

        Families.Setup(repository => repository.FindAsync(family.Id, It.IsAny<CancellationToken>())).ReturnsAsync(family);

        return family;
    }
}
