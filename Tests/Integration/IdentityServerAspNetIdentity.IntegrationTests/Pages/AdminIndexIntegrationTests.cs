using System.Net;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class AdminIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    // ── Step 1: Bare page rendering ──────────────────────────────────────

    [Fact]
    public async Task Get_AdminIndex_Returns200()
    {
        // Act
        var response = await _client.GetAsync("/Admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_AdminIndex_ContainsAdminHeading()
    {
        // Act
        var response = await _client.GetAsync("/Admin");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Admin");
    }

    // ── Step 2: Dashboard links ──────────────────────────────────────────

    [Fact]
    public async Task Get_AdminIndex_ContainsUsersLink()
    {
        // Act
        var response = await _client.GetAsync("/Admin");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var usersLink = document.QuerySelector("a[href*='/Admin/Users']");
        usersLink.Should().NotBeNull("page should contain a link to the Users admin section");
        usersLink!.TextContent.Should().Contain("Users");
    }

    [Fact]
    public async Task Get_AdminIndex_ContainsClientsLink()
    {
        // Act
        var response = await _client.GetAsync("/Admin");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert — Clients route is defined as /admin/clients (lowercase)
        var clientsLink = document.QuerySelector("a[href*='/admin/clients'], a[href*='/Admin/Clients']");
        clientsLink.Should().NotBeNull("page should contain a link to the Clients admin section");
    }

    [Fact]
    public async Task Get_AdminIndex_ContainsRolesLink()
    {
        // Act  — **[ASSUMED]** Roles link is included even though the Roles page is a placeholder,
        // mirroring the existing sidebar which already shows a Roles entry.
        var response = await _client.GetAsync("/Admin");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rolesLink = document.QuerySelector("a[href*='Roles'], [data-title='Roles']");
        rolesLink.Should().NotBeNull("page should contain a link or reference to the Roles admin section");
    }

    // ── Step 3: Sidebar contains Admin dashboard link ────────────────────

    [Fact]
    public async Task Get_AdminIndex_SidebarContainsDashboardLink()
    {
        // Act
        var response = await _client.GetAsync("/Admin");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert — sidebar should have a dashboard/home item linking to /Admin
        var sidebar = document.QuerySelector(".sidebar");
        sidebar.Should().NotBeNull();

        var dashboardItem = sidebar!.QuerySelector("li[data-title='Dashboard']");
        dashboardItem.Should().NotBeNull("sidebar should contain a Dashboard link");
        dashboardItem!.GetAttribute("data-url").Should().Contain("/Admin");
    }

    [Fact]
    public async Task Get_UsersIndex_SidebarContainsDashboardLink()
    {
        // Verify the sidebar update is visible from other admin pages too
        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var sidebar = document.QuerySelector(".sidebar");
        sidebar.Should().NotBeNull();

        var dashboardItem = sidebar!.QuerySelector("li[data-title='Dashboard']");
        dashboardItem.Should().NotBeNull("sidebar should contain a Dashboard link on all pages");
    }
}
