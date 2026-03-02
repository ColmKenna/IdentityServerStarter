using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ApiScopesEditIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiScopesEditIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private int SeedApiScope(
        string name = "orders.read",
        string? displayName = "Orders Read",
        string? description = "Read orders",
        bool enabled = true,
        params string[] userClaims)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var apiScope = new ApiScope
        {
            Name = name,
            DisplayName = displayName,
            Description = description,
            Enabled = enabled,
            UserClaims = userClaims.Select(claimType => new ApiScopeClaim { Type = claimType }).ToList()
        };

        context.ApiScopes.Add(apiScope);
        context.SaveChanges();
        return apiScope.Id;
    }

    private async Task SeedUserClaimTypeAsync(string claimType, string claimValue = "value")
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var suffix = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            UserName = $"scope-claims-{suffix}",
            Email = $"scope-claims-{suffix}@test.local",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, "Pass123$");
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
        }

        var claimResult = await userManager.AddClaimAsync(user, new System.Security.Claims.Claim(claimType, claimValue));
        if (!claimResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claim: {string.Join(", ", claimResult.Errors.Select(e => e.Description))}");
        }
    }

    [Fact]
    public async Task Get_EditApiScope_ValidId_Returns200()
    {
        var id = SeedApiScope();

        var response = await _client.GetAsync($"/Admin/ApiScopes/{id}/Edit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_EditApiScope_InvalidId_Returns404()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/99999/Edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_EditApiScope_RendersExistingValues()
    {
        var id = SeedApiScope(name: "orders.read", displayName: "Orders Read", description: "Read orders", enabled: true);

        var response = await _client.GetAsync($"/Admin/ApiScopes/{id}/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("input[name='Input.Name']")!.GetAttribute("value").Should().Be("orders.read");
        document.QuerySelector("input[name='Input.DisplayName']")!.GetAttribute("value").Should().Be("Orders Read");
        document.QuerySelector("textarea[name='Input.Description']")!.TextContent.Should().Contain("Read orders");
    }

    [Fact]
    public async Task Get_EditApiScope_RendersCkTabsWithUserClaimsAsSecondTab()
    {
        var id = SeedApiScope();

        var response = await _client.GetAsync($"/Admin/ApiScopes/{id}/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("ck-tabs").Should().NotBeNull();
        document.QuerySelector("ck-tab[label='Basic Information']").Should().NotBeNull();
        document.QuerySelector("ck-tab[label='User Claims']").Should().NotBeNull();
    }

    [Fact]
    public async Task Get_EditApiScope_BasicInformation_UsesProfileCardLayout()
    {
        var id = SeedApiScope();

        var response = await _client.GetAsync($"/Admin/ApiScopes/{id}/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("ck-tab[label='Basic Information'] .profile-card").Should().NotBeNull();
        document.QuerySelector("ck-tab[label='Basic Information'] .profile-card__header").Should().NotBeNull();
        document.QuerySelector("ck-tab[label='Basic Information'] .profile-card__actions").Should().NotBeNull();
        document.QuerySelector("ck-tab[label='Basic Information'] .form-group.form-switch-row").Should().NotBeNull();
    }

    [Fact]
    public async Task Get_EditApiScope_UserClaimsTab_ShowsAppliedAndAvailableClaims()
    {
        var id = SeedApiScope(userClaims: new[] { "department" });
        await SeedUserClaimTypeAsync("department", "engineering");
        await SeedUserClaimTypeAsync("location", "dublin");

        var response = await _client.GetAsync($"/Admin/ApiScopes/{id}/Edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("#applied-user-claims-table").Should().NotBeNull();
        document.Body!.TextContent.Should().Contain("department");

        var availableSelect = document.QuerySelector("#available-user-claims-select");
        availableSelect.Should().NotBeNull();
        availableSelect!.TextContent.Should().Contain("location");
        availableSelect.TextContent.Should().NotContain("department");
    }

    [Fact]
    public async Task PostEdit_ValidInput_Returns302RedirectToEdit()
    {
        var id = SeedApiScope();
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            $"/Admin/ApiScopes/{id}/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["Input.Name"] = "orders.write",
                ["Input.DisplayName"] = "Orders Write",
                ["Input.Description"] = "Write orders",
                ["Input.Enabled"] = "false"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain($"/Admin/ApiScopes/{id}/Edit");
    }

    [Fact]
    public async Task PostEdit_ValidInput_UpdatesApiScope()
    {
        var id = SeedApiScope();

        await _client.PostAsync(
            $"/Admin/ApiScopes/{id}/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["Input.Name"] = "orders.write",
                ["Input.DisplayName"] = "Orders Write",
                ["Input.Description"] = "Write orders",
                ["Input.Enabled"] = "false"
            }));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updated = await context.ApiScopes.FindAsync(id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("orders.write");
        updated.DisplayName.Should().Be("Orders Write");
        updated.Description.Should().Be("Write orders");
        updated.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task PostEdit_DuplicateName_Returns200AndValidationMessage()
    {
        var firstId = SeedApiScope(name: "orders.read");
        var secondId = SeedApiScope(name: "orders.write");

        var response = await _client.PostAsync(
            $"/Admin/ApiScopes/{secondId}/Edit",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = secondId.ToString(),
                ["Input.Name"] = "orders.read",
                ["Input.DisplayName"] = "Duplicate Name",
                ["Input.Description"] = "Duplicate",
                ["Input.Enabled"] = "true"
            }));
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("already exists");
    }

    [Fact]
    public async Task PostAddClaim_ValidSelection_Returns302Redirect()
    {
        var id = SeedApiScope();
        await SeedUserClaimTypeAsync("department", "engineering");
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            $"/Admin/ApiScopes/{id}/Edit?handler=AddClaim",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["SelectedClaimType"] = "department"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain($"/Admin/ApiScopes/{id}/Edit");
    }

    [Fact]
    public async Task PostAddClaim_ValidSelection_PersistsApiScopeClaim()
    {
        var id = SeedApiScope();
        await SeedUserClaimTypeAsync("department", "engineering");

        await _client.PostAsync(
            $"/Admin/ApiScopes/{id}/Edit?handler=AddClaim",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["SelectedClaimType"] = "department"
            }));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updated = await context.ApiScopes
            .Include(apiScope => apiScope.UserClaims)
            .SingleAsync(apiScope => apiScope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("department");
    }

    [Fact]
    public async Task PostRemoveClaim_ValidSelection_RemovesApiScopeClaim()
    {
        var id = SeedApiScope(userClaims: new[] { "department", "location" });

        await _client.PostAsync(
            $"/Admin/ApiScopes/{id}/Edit?handler=RemoveClaim",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = id.ToString(),
                ["RemoveClaimType"] = "department"
            }));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updated = await context.ApiScopes
            .Include(apiScope => apiScope.UserClaims)
            .SingleAsync(apiScope => apiScope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().NotContain("department");
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("location");
    }
}
