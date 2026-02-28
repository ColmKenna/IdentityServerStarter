using System.Net;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class RolesIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RolesIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    #region Helpers

    private void SeedRoles(params string[] roleNames)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in roleNames)
        {
            if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
            }
        }
    }

    #endregion

    // ── Step 1: Bare page rendering ──────────────────────────────────────

    [Fact]
    public async Task Get_RolesIndex_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_RolesIndex_ContainsRolesHeading()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Roles");
    }

    // ── Step 2: GET data display ─────────────────────────────────────────

    [Fact]
    public async Task Get_RolesExist_ReturnsPageWithRolesTable()
    {
        // Arrange
        SeedRoles("Admin", "Editor", "Viewer");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var table = document.QuerySelector("#roles-table");
        table.Should().NotBeNull("page should contain a roles table");
    }

    [Fact]
    public async Task Get_RolesExist_TableContainsCorrectNumberOfRows()
    {
        // Arrange
        SeedRoles("Admin", "Editor", "Viewer");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("#roles-table tbody tr");
        rows.Length.Should().Be(3);
    }

    [Fact]
    public async Task Get_RolesExist_TableDisplaysRoleNames()
    {
        // Arrange
        SeedRoles("Admin", "Editor");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("#roles-table tbody tr");
        var roleNames = rows.Select(r => r.QuerySelector("td")?.TextContent.Trim()).ToList();
        roleNames.Should().Contain("Admin");
        roleNames.Should().Contain("Editor");
    }

    // ── Step 3: GET edge cases ───────────────────────────────────────────

    [Fact]
    public async Task Get_NoRolesExist_ShowsEmptyStateMessage()
    {
        // Act — no roles seeded
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var emptyMessage = document.QuerySelector(".alert-info");
        emptyMessage.Should().NotBeNull("page should show an info message when no roles exist");
        emptyMessage!.TextContent.Should().Contain("No roles");
    }

    [Fact]
    public async Task Get_NoRolesExist_DoesNotRenderTable()
    {
        // Act — no roles seeded
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var table = document.QuerySelector("#roles-table");
        table.Should().BeNull("table should not render when there are no roles");
    }
}
