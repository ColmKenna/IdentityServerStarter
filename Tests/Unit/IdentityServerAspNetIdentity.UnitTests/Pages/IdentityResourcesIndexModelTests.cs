using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class IdentityResourcesIndexModelTests
{
    private static ConfigurationDbContext CreateDbContext()
    {
        var storeOptions = new ConfigurationStoreOptions();
        var serviceProvider = new ServiceCollection()
            .AddSingleton(storeOptions)
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .UseApplicationServiceProvider(serviceProvider)
            .Options;

        return new ConfigurationDbContext(options);
    }

    [Fact]
    public async Task OnGetAsync_InitializesIdentityResourcesCollection()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.IdentityResources.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_IdentityResourcesExist_PopulatesIdentityResources()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentityResources.AddRange(
            new IdentityResource { Name = "profile", DisplayName = "Profile", Description = "User profile", Enabled = true },
            new IdentityResource { Name = "email", DisplayName = "Email", Description = "User email", Enabled = false });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.IdentityResources.Should().HaveCount(2);
        pageModel.IdentityResources.Select(r => r.Name).Should().Contain(new[] { "profile", "email" });
    }

    [Fact]
    public async Task OnGetAsync_IdentityResourcesAreOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentityResources.AddRange(
            new IdentityResource { Name = "zeta", DisplayName = "Zeta", Description = "Zeta description", Enabled = true },
            new IdentityResource { Name = "alpha", DisplayName = "Alpha", Description = "Alpha description", Enabled = true },
            new IdentityResource { Name = "middle", DisplayName = "Middle", Description = "Middle description", Enabled = true });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.IdentityResources.Select(r => r.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_NoIdentityResourcesExist_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.IdentityResources.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_NullDisplayNameOrDescription_MapsToEmptyStrings()
    {
        await using var dbContext = CreateDbContext();
        dbContext.IdentityResources.Add(new IdentityResource
        {
            Name = "profile",
            DisplayName = null,
            Description = null,
            Enabled = true
        });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.IdentityResources.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.IdentityResources.Should().ContainSingle();
        pageModel.IdentityResources[0].DisplayName.Should().BeEmpty();
        pageModel.IdentityResources[0].Description.Should().BeEmpty();
    }
}
