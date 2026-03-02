using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using IdentityServer.EF.DataAccess.DataMigrations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.UnitTests.Pages;

public class ApiScopesEditModelTests
{
    private static ConfigurationDbContext CreateConfigurationDbContext()
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

    private static ApplicationDbContext CreateApplicationDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel CreatePageModel(
        ConfigurationDbContext configurationDbContext,
        ApplicationDbContext applicationDbContext)
    {
        return new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel(configurationDbContext, applicationDbContext)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGetAsync_ApiScopeExists_ReturnsPageAndPopulatesInput()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = true
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;

        var result = await pageModel.OnGetAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.Input.Name.Should().Be("orders.read");
        pageModel.Input.DisplayName.Should().Be("Orders Read");
        pageModel.Input.Description.Should().Be("Read orders");
        pageModel.Input.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_ApiScopeMissing_ReturnsNotFound()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = 999;

        var result = await pageModel.OnGetAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ValidInput_UpdatesApiScope()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = true
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel.ApiScopeInputModel
        {
            Name = "orders.write",
            DisplayName = "Orders Write",
            Description = "Write orders",
            Enabled = false
        };

        await pageModel.OnPostAsync();

        var updated = await configurationDbContext.ApiScopes.FindAsync(id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("orders.write");
        updated.DisplayName.Should().Be("Orders Write");
        updated.Description.Should().Be("Write orders");
        updated.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostAsync_ValidInput_RedirectsToEditPage()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope { Name = "orders.read", Enabled = true });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Orders Read",
            Description = "Read orders",
            Enabled = true
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var redirect = (RedirectToPageResult)result;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues.Should().ContainKey("id");
        redirect.RouteValues!["id"].Should().Be(id);
    }

    [Fact]
    public async Task OnPostAsync_ModelStateInvalid_ReturnsPage()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope { Name = "orders.read", Enabled = true });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.ModelState.AddModelError("Input.Name", "Name is required");
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel.ApiScopeInputModel
        {
            Name = string.Empty
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
    }

    [Fact]
    public async Task OnPostAsync_ApiScopeMissing_ReturnsNotFound()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = 999;
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            Enabled = true
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_DuplicateName_ReturnsPageWithModelError()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.AddRange(
            new ApiScope { Name = "orders.read", Enabled = true },
            new ApiScope { Name = "orders.write", Enabled = true });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Where(s => s.Name == "orders.write").Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.Input = new IdentityServerAspNetIdentity.Pages.Admin.ApiScopes.EditModel.ApiScopeInputModel
        {
            Name = "orders.read",
            DisplayName = "Duplicate Name",
            Description = "Should fail",
            Enabled = true
        };

        var result = await pageModel.OnPostAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey("Input.Name");
    }

    [Fact]
    public async Task OnGetAsync_ApiScopeHasUserClaims_PopulatesAppliedUserClaims()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            Enabled = true,
            UserClaims = new List<ApiScopeClaim>
            {
                new() { Type = "department" },
                new() { Type = "location" }
            }
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;

        await pageModel.OnGetAsync();

        pageModel.AppliedUserClaims.Should().Contain(new[] { "department", "location" });
    }

    [Fact]
    public async Task OnGetAsync_SystemUserClaimsExist_PopulatesAvailableUserClaimsExcludingApplied()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            Enabled = true,
            UserClaims = new List<ApiScopeClaim>
            {
                new() { Type = "department" }
            }
        });

        applicationDbContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "engineering" },
            new IdentityUserClaim<string> { UserId = "u2", ClaimType = "location", ClaimValue = "dublin" },
            new IdentityUserClaim<string> { UserId = "u3", ClaimType = "region", ClaimValue = "eu" });
        await configurationDbContext.SaveChangesAsync();
        await applicationDbContext.SaveChangesAsync();

        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();
        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;

        await pageModel.OnGetAsync();

        pageModel.AvailableUserClaims.Should().Contain("location");
        pageModel.AvailableUserClaims.Should().Contain("region");
        pageModel.AvailableUserClaims.Should().NotContain("department");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ValidSelection_AddsClaimAndRedirects()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope { Name = "orders.read", Enabled = true });
        applicationDbContext.UserClaims.Add(new IdentityUserClaim<string>
        {
            UserId = "u1",
            ClaimType = "department",
            ClaimValue = "engineering"
        });
        await configurationDbContext.SaveChangesAsync();
        await applicationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.SelectedClaimType = "department";

        var result = await pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var updated = await configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .SingleAsync(scope => scope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("department");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_DuplicateClaim_ReturnsPageWithModelError()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            Enabled = true,
            UserClaims = new List<ApiScopeClaim> { new() { Type = "department" } }
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.SelectedClaimType = "department";

        var result = await pageModel.OnPostAddClaimAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey("SelectedClaimType");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ValidSelection_RemovesClaimAndRedirects()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            Enabled = true,
            UserClaims = new List<ApiScopeClaim>
            {
                new() { Type = "department" },
                new() { Type = "location" }
            }
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.RemoveClaimType = "department";

        var result = await pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<RedirectToPageResult>();
        var updated = await configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .SingleAsync(scope => scope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().NotContain("department");
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("location");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_NoClaimSelected_ReturnsPageWithModelError()
    {
        await using var configurationDbContext = CreateConfigurationDbContext();
        await using var applicationDbContext = CreateApplicationDbContext();
        configurationDbContext.ApiScopes.Add(new ApiScope
        {
            Name = "orders.read",
            Enabled = true,
            UserClaims = new List<ApiScopeClaim> { new() { Type = "department" } }
        });
        await configurationDbContext.SaveChangesAsync();
        var id = await configurationDbContext.ApiScopes.Select(s => s.Id).SingleAsync();

        var pageModel = CreatePageModel(configurationDbContext, applicationDbContext);
        pageModel.Id = id;
        pageModel.RemoveClaimType = string.Empty;

        var result = await pageModel.OnPostRemoveClaimAsync();

        result.Should().BeOfType<PageResult>();
        pageModel.ModelState.Should().ContainKey("RemoveClaimType");
    }
}
