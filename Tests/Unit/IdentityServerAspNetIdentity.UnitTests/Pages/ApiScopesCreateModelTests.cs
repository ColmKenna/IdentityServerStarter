using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesCreateModelTests
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

    private static IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel CreatePageModel(ConfigurationDbContext dbContext)
    {
        return new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel(dbContext)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public void OnGet_InitializesInputModel()
    {
        using var dbContext = CreateDbContext();
        var pageModel = CreatePageModel(dbContext);

        pageModel.OnGet();

        pageModel.Input.Should().NotBeNull();
    }

    [Fact]
    public async Task OnPostAsync_ValidInput_CreatesApiScope()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = CreatePageModel(dbContext);
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = true
        };

        await pageModel.OnPostAsync();

        var created = await dbContext.ApiScopes.SingleOrDefaultAsync(s => s.Name == "orders.read");
        created.Should().NotBeNull();
        created!.DisplayName.Should().Be("Orders Read");
        created.Description.Should().Be("Read orders");
        created.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAsync_ValidInput_RedirectsToEditPage()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = CreatePageModel(dbContext);
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel.ApiScopeInputModel
        {
            Name = "orders.write",
            DisplayName = "Orders Write",
            Description = "Write orders",
            Enabled = false
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        await using var dbContext = CreateDbContext();
        var pageModel = CreatePageModel(dbContext);
        pageModel.ModelState.AddModelError("Input.Name", "Name is required");
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel.ApiScopeInputModel
        {
            Name = string.Empty
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_DuplicateName_ReturnsPageWithModelError()
    {
        await using var dbContext = CreateDbContext();
        dbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            DisplayName = "Existing Scope",
            Enabled = true
        });
        await dbContext.SaveChangesAsync();

        var pageModel = CreatePageModel(dbContext);
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.CreateModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Duplicate Scope",
            Enabled = true
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey("Input.Name");
    }
}
