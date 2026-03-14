using System.Net;
using FluentAssertions;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Roles;

public class IndexModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IndexModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await unauthClient.GetAsync("/Admin/Roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await nonAdminClient.GetAsync("/Admin/Roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldShowRoles_WhenAdmin()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Clear and seed roles
        var roles = await roleManager.Roles.ToListAsync();
        foreach (var r in roles) await roleManager.DeleteAsync(r);
        
        await roleManager.CreateAsync(new IdentityRole("Manager"));
        await roleManager.CreateAsync(new IdentityRole("Viewer"));

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);
        rows.Should().Contain(r => r.TextContent.Contains("Manager"));
        rows.Should().Contain(r => r.TextContent.Contains("Viewer"));
    }

    [Fact]
    public async Task Get_ShouldShowEmptyState_WhenNoRolesExist()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var roles = await roleManager.Roles.ToListAsync();
        foreach (var r in roles) await roleManager.DeleteAsync(r);

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No roles found.");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
