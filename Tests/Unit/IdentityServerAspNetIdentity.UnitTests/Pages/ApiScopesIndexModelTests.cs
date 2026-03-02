using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesIndexModelTests
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
    public async Task OnGetAsync_InitializesApiScopesCollection()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.ApiScopes.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_ApiScopesExist_PopulatesApiScopes()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApiScopes.AddRange(
            new ApiScope { Name = "orders.read", DisplayName = "Orders Read", Description = "Read orders", Enabled = true },
            new ApiScope { Name = "orders.write", DisplayName = "Orders Write", Description = "Write orders", Enabled = false });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.ApiScopes.Should().HaveCount(2);
        pageModel.ApiScopes.Select(s => s.Name).Should().Contain(new[] { "orders.read", "orders.write" });
    }

    [Fact]
    public async Task OnGetAsync_ApiScopesAreOrderedByName()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApiScopes.AddRange(
            new ApiScope { Name = "zeta", DisplayName = "Zeta", Description = "Zeta desc", Enabled = true },
            new ApiScope { Name = "alpha", DisplayName = "Alpha", Description = "Alpha desc", Enabled = true },
            new ApiScope { Name = "middle", DisplayName = "Middle", Description = "Middle desc", Enabled = true });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.ApiScopes.Select(s => s.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task OnGetAsync_NoApiScopesExist_ReturnsEmptyList()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.ApiScopes.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_NullDisplayNameOrDescription_MapsToEmptyStrings()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApiScopes.Add(new ApiScope
        {
            Name = "api1",
            DisplayName = null,
            Description = null,
            Enabled = true
        });
        await dbContext.SaveChangesAsync();

        var pageModel = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.IndexModel(dbContext);

        await pageModel.OnGetAsync();

        pageModel.ApiScopes.Should().ContainSingle();
        pageModel.ApiScopes[0].DisplayName.Should().BeEmpty();
        pageModel.ApiScopes[0].Description.Should().BeEmpty();
    }
}
