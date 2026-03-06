using System.Net;
using System.Security.Claims;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ClaimsIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClaimsIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

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

    private async Task AddUserClaimAsync(string userId, string claimType, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User '{userId}' not found.");
        var result = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claim: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    [Fact]
    public async Task Get_ClaimsIndex_Returns200()
    {
        var response = await _client.GetAsync("/Admin/Claims");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ClaimsIndex_ContainsClaimsHeading()
    {
        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Claims");
    }

    [Fact]
    public async Task Get_ClaimTypesExist_RendersClaimsTable()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");
        await AddUserClaimAsync(userId, "location", "dublin");

        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().NotBeNull("page should render a claims table");
    }

    [Fact]
    public async Task Get_ClaimTypesExist_RendersOneRowPerDistinctClaimType()
    {
        var user1Id = await SeedUserAsync("claims-user");
        var user2Id = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(user1Id, "department", "engineering");
        await AddUserClaimAsync(user2Id, "department", "sales");
        await AddUserClaimAsync(user2Id, "location", "dublin");

        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var rows = document.QuerySelectorAll("ck-responsive-table ck-responsive-tbody ck-responsive-row");
        rows.Length.Should().Be(2);
    }

    [Fact]
    public async Task Get_ClaimTypesExist_RowContainsManageUsersLink()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var manageLink = document.QuerySelector("a[href*='/Admin/Claims/Edit'][href*='claimType=department']");
        manageLink.Should().NotBeNull("each claim row should contain a manage users link");
        manageLink!.TextContent.Should().Contain("Manage Users");
    }

    [Fact]
    public async Task Get_NoClaimsExist_ShowsEmptyStateMessage()
    {
        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var emptyMessage = document.QuerySelector(".alert-info");
        emptyMessage.Should().NotBeNull("page should show an info message when no claims exist");
        emptyMessage!.TextContent.Should().Contain("No claims");
    }

    [Fact]
    public async Task Get_NoClaimsExist_DoesNotRenderTable()
    {
        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("ck-responsive-table");
        table.Should().BeNull("table should not render when no claim types exist");
    }

    [Fact]
    public async Task Get_ClaimsPage_DoesNotRenderClaimValues()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var pageText = document.Body!.TextContent;
        pageText.Should().Contain("department");
        pageText.Should().NotContain("engineering");
    }
}
