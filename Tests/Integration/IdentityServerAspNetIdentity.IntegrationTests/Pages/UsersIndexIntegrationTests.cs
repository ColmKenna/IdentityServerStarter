using IdentityServerAspNetIdentity.Models;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class UsersIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UsersIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Helpers

    private async Task<string> SeedUserAsync(string usernamePrefix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var user = new ApplicationUser
        {
            UserName = $"{usernamePrefix}-{uniqueSuffix}",
            Email = $"{usernamePrefix}-{uniqueSuffix}@test.local",
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Pass123$");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user.Id;
    }

    #endregion

    // ── Step 1: Bare page rendering ──────────────────────────────────────

    [Fact]
    public async Task Get_UsersIndex_Returns200()
    {
        var response = await _client.GetAsync("/Admin/Users");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_UsersIndex_ContainsUsersHeading()
    {
        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Users");
    }

    // ── Step 2: GET data display ─────────────────────────────────────────

    [Fact]
    public async Task Get_UsersExist_ReturnsPageWithUsersTable()
    {
        await SeedUserAsync("tableuser");

        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().NotBeNull("page should contain a users table when users exist");
    }

    [Fact]
    public async Task Get_UsersExist_TableDisplaysUserData()
    {
        await SeedUserAsync("displayuser");

        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        rows.Length.Should().BeGreaterThanOrEqualTo(1);

        var firstRowText = rows[0].TextContent;
        firstRowText.Should().Contain("displayuser");
    }

    // ── Step 3: GET edge cases ───────────────────────────────────────────

    [Fact]
    public async Task Get_NoUsersExist_ShowsEmptyStateMessage()
    {
        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var emptyMessage = document.QuerySelector(".alert-info");
        emptyMessage.Should().NotBeNull("page should show an info message when no users exist");
        emptyMessage!.TextContent.Should().Contain("No users");
    }

    [Fact]
    public async Task Get_UsersIndex_HasValidHtmlStructure()
    {
        var response = await _client.GetAsync("/Admin/Users");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.DocumentElement.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }
}
