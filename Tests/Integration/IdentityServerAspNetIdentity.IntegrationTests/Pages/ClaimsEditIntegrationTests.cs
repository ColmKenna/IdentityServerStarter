using System.Net;
using System.Security.Claims;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ClaimsEditIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClaimsEditIntegrationTests()
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
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException($"User '{userId}' not found.");
        }

        var result = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claim: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    private async Task<bool> UserHasClaimAsync(string userId, string claimType, string claimValue)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return false;
        }

        var claims = await userManager.GetClaimsAsync(user);
        return claims.Any(c => c.Type == claimType && c.Value == claimValue);
    }

    [Fact]
    public async Task Get_ClaimEdit_ValidClaimType_Returns200()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ClaimEdit_ContainsClaimTypeHeading()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("department");
    }

    [Fact]
    public async Task Get_ClaimEdit_AssignmentsExist_RendersUsersTable()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var table = document.QuerySelector("#users-in-claim-table");
        table.Should().NotBeNull("page should render a users in claim table");
    }

    [Fact]
    public async Task Get_ClaimEdit_AssignmentsExist_RendersUserAndClaimValueColumns()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var headerText = document.QuerySelector("#users-in-claim-table thead")!.TextContent;
        headerText.Should().Contain("Username");
        headerText.Should().Contain("Claim Value");
    }

    [Fact]
    public async Task Get_ClaimEdit_AssignmentsExist_RendersOneRowPerAssignedUser()
    {
        var user1Id = await SeedUserAsync("claims-user");
        var user2Id = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(user1Id, "department", "engineering");
        await AddUserClaimAsync(user2Id, "department", "sales");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var rows = document.QuerySelectorAll("#users-in-claim-table tbody tr");
        rows.Length.Should().Be(2);
    }

    [Fact]
    public async Task Get_ClaimEdit_MissingClaimType_Returns400()
    {
        var response = await _client.GetAsync("/Admin/Claims/Edit");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_ClaimEdit_UnknownClaimType_Returns404()
    {
        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ClaimEdit_SingleAssignedUser_RemoveButtonContainsLastUserWarningText()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var warning = document.QuerySelector(".last-user-warning");
        warning.Should().NotBeNull("single assignment should show last user warning");
        warning!.TextContent.Should().Contain("different value");
        warning.TextContent.Should().Contain("keep this claim");
    }

    [Fact]
    public async Task Get_ClaimEdit_RendersAddUserFormWithUserSelectAndClaimValueInput()
    {
        var assignedUserId = await SeedUserAsync("claims-user");
        var availableUserId = await SeedUserAsync("available-user");
        await AddUserClaimAsync(assignedUserId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("#add-user-claim-form").Should().NotBeNull();
        document.QuerySelector("#add-user-claim-form select[name='SelectedUserId']").Should().NotBeNull();
        document.QuerySelector("#add-user-claim-form input[name='NewClaimValue']").Should().NotBeNull();
        document.Body!.TextContent.Should().Contain("available-user");
    }

    [Fact]
    public async Task Get_ClaimEdit_BooleanAssignments_DefaultsAddClaimValueToTrue()
    {
        var assignedUser1Id = await SeedUserAsync("claims-user");
        var assignedUser2Id = await SeedUserAsync("claims-user");
        var availableUserId = await SeedUserAsync("available-user");
        await AddUserClaimAsync(assignedUser1Id, "feature-enabled", "true");
        await AddUserClaimAsync(assignedUser2Id, "feature-enabled", "false");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=feature-enabled");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var input = document.QuerySelector("#add-user-claim-form input[name='NewClaimValue']");
        input.Should().NotBeNull();
        input!.GetAttribute("value").Should().Be("true");
    }

    [Fact]
    public async Task Get_ClaimEdit_RendersRemoveFormPerAssignedRow()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var removeForm = document.QuerySelector("#users-in-claim-table form[id^='remove-user-claim-form-']");
        removeForm.Should().NotBeNull("each assigned row should contain remove form");
    }

    [Fact]
    public async Task Get_ClaimEdit_RendersBackToClaimsLink()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var backLink = document.QuerySelector("a[href*='/Admin/Claims']");
        backLink.Should().NotBeNull();
        backLink!.TextContent.Should().Contain("Back to Claims");
    }

    [Fact]
    public async Task PostAddUser_ValidInput_Returns302RedirectToEdit()
    {
        var assignedUserId = await SeedUserAsync("claims-user");
        var newUserId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(assignedUserId, "department", "engineering");

        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["SelectedUserId"] = newUserId,
                ["NewClaimValue"] = "sales"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Admin/Claims/Edit");
        response.Headers.Location!.ToString().Should().Contain("claimType=department");
    }

    [Fact]
    public async Task PostAddUser_ValidInput_PersistsClaimAssignment()
    {
        var assignedUserId = await SeedUserAsync("claims-user");
        var newUserId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(assignedUserId, "department", "engineering");

        await _client.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["SelectedUserId"] = newUserId,
                ["NewClaimValue"] = "sales"
            }));

        var hasClaim = await UserHasClaimAsync(newUserId, "department", "sales");
        hasClaim.Should().BeTrue();
    }

    [Fact]
    public async Task PostAddUser_MissingValue_Returns200AndValidationMessage()
    {
        var assignedUserId = await SeedUserAsync("claims-user");
        var newUserId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(assignedUserId, "department", "engineering");

        var response = await _client.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["SelectedUserId"] = newUserId,
                ["NewClaimValue"] = string.Empty
            }));
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("Claim value is required");
    }

    [Fact]
    public async Task PostAddUser_DuplicateAssignment_Returns200AndValidationMessage()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["SelectedUserId"] = userId,
                ["NewClaimValue"] = "duplicate"
            }));
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("already has this claim type");
    }

    [Fact]
    public async Task PostRemoveUser_ValidInput_RemovesAssignment()
    {
        var user1Id = await SeedUserAsync("claims-user");
        var user2Id = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(user1Id, "department", "engineering");
        await AddUserClaimAsync(user2Id, "department", "sales");

        await _client.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["RemoveUserId"] = user1Id,
                ["RemoveClaimValue"] = "engineering"
            }));

        var hasClaim = await UserHasClaimAsync(user1Id, "department", "engineering");
        hasClaim.Should().BeFalse();
    }

    [Fact]
    public async Task PostRemoveUser_NonLastUser_RedirectsBackToEdit()
    {
        var user1Id = await SeedUserAsync("claims-user");
        var user2Id = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(user1Id, "department", "engineering");
        await AddUserClaimAsync(user2Id, "department", "sales");

        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["RemoveUserId"] = user1Id,
                ["RemoveClaimValue"] = "engineering"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Admin/Claims/Edit");
    }

    [Fact]
    public async Task PostRemoveUser_LastUser_RedirectsToClaimsIndex()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["RemoveUserId"] = userId,
                ["RemoveClaimValue"] = "engineering"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Admin/Claims");
    }

    [Fact]
    public async Task PostRemoveUser_LastUser_ClaimTypeNoLongerAppearsOnClaimsIndex()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        await _client.PostAsync(
            "/Admin/Claims/Edit?claimType=department&handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["ClaimType"] = "department",
                ["RemoveUserId"] = userId,
                ["RemoveClaimValue"] = "engineering"
            }));

        var indexResponse = await _client.GetAsync("/Admin/Claims");
        var indexDocument = await AngleSharpHelpers.GetDocumentAsync(indexResponse);

        indexDocument.Body!.TextContent.Should().NotContain("department");
    }

    [Fact]
    public async Task Get_ClaimEdit_ContainsUserEditLinkPerRow()
    {
        var userId = await SeedUserAsync("claims-user");
        await AddUserClaimAsync(userId, "department", "engineering");

        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=department");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        var userLink = document.QuerySelector("#users-in-claim-table a[href*='/Admin/Users/']");
        userLink.Should().NotBeNull("assigned users should link to user edit page");
    }
}
