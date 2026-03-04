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

    private async Task SeedRolesAsync(params string[] roleNames)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new Exception("Role Save Failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
                }
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
        await SeedRolesAsync("Admin", "Editor", "Viewer");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var table = document.QuerySelector("ck-responsive-table");
        table.Should().NotBeNull("page should contain a roles table");
    }

    [Fact]
    public async Task Get_RolesExist_TableContainsCorrectNumberOfRows()
    {
        // Arrange
        await SeedRolesAsync("Admin", "Editor", "Viewer");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        rows.Length.Should().Be(3);
    }

    [Fact]
    public async Task Get_RolesExist_TableDisplaysRoleNames()
    {
        // Arrange
        await SeedRolesAsync("Admin", "Editor");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityServer.EF.DataAccess.DataMigrations.ApplicationDbContext>();
            var roleCount = db.Roles.Count();
            if (roleCount != 2) throw new Exception("Database roles count is " + roleCount + " after SeedRoles!");
            var roleNamesInDb = string.Join(", ", db.Roles.Select(r => r.Name));
            if (!roleNamesInDb.Contains("Admin")) throw new Exception("Admin not in DB directly! " + roleNamesInDb);
        }

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        var roleNames = rows.Select(r => r.QuerySelector("ck-responsive-col")?.TextContent.Trim()).ToList();
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
        var table = document.QuerySelector("ck-responsive-table");
        table.Should().BeNull("table should not render when there are no roles");
    }
}
