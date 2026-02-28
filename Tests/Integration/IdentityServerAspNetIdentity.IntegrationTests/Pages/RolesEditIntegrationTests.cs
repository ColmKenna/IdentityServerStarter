using System.Net;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class RolesEditIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RolesEditIntegrationTests()
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

    private string SeedRole(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
        {
            roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
        }

        var role = roleManager.FindByNameAsync(roleName).GetAwaiter().GetResult()!;
        return role.Id;
    }

    private string SeedUser(string username, string email)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        var result = userManager.CreateAsync(user, "Pass123$").GetAwaiter().GetResult();
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user.Id;
    }

    private void AddUserToRole(string userId, string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = userManager.FindByIdAsync(userId).GetAwaiter().GetResult()!;
        userManager.AddToRoleAsync(user, roleName).GetAwaiter().GetResult();
    }

    #endregion

    // ── Step 1: Bare page rendering + GET data display ───────────────────

    [Fact]
    public async Task Get_RoleExists_Returns200()
    {
        // Arrange
        var roleId = SeedRole("Editor");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_RoleExists_ContainsRoleNameHeading()
    {
        // Arrange
        var roleId = SeedRole("Moderator");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Moderator");
    }

    [Fact]
    public async Task Get_RoleWithUsers_DisplaysUsersInTable()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId1 = SeedUser("alice", "alice@test.com");
        var userId2 = SeedUser("bob", "bob@test.com");
        AddUserToRole(userId1, "Editor");
        AddUserToRole(userId2, "Editor");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var rows = document.QuerySelectorAll("#users-in-role-table tbody tr");
        rows.Length.Should().Be(2);
    }

    [Fact]
    public async Task Get_RoleWithUsers_DisplaysUsernames()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("alice", "alice@test.com");
        AddUserToRole(userId, "Editor");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var content = document.QuerySelector("#users-in-role-table")!.TextContent;
        content.Should().Contain("alice");
    }

    // ── Step 2: GET edge cases ───────────────────────────────────────────

    [Fact]
    public async Task Get_RoleNotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/Admin/Roles/nonexistent-id");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_RoleWithNoUsers_ShowsEmptyStateMessage()
    {
        // Arrange
        var roleId = SeedRole("EmptyRole");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var emptyMessage = document.QuerySelector("#no-users-message");
        emptyMessage.Should().NotBeNull("should display a message when no users in role");
        emptyMessage!.TextContent.Should().Contain("No users");
    }

    [Fact]
    public async Task Get_RoleWithNoUsers_DoesNotRenderUsersTable()
    {
        // Arrange
        var roleId = SeedRole("EmptyRole2");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var table = document.QuerySelector("#users-in-role-table");
        table.Should().BeNull("table should not render when no users are in role");
    }

    // ── Step 3: Form rendering ───────────────────────────────────────────

    [Fact]
    public async Task Get_RoleExists_ContainsAddUserForm()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        SeedUser("avail-user", "avail@test.com");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var form = document.QuerySelector("#add-user-form");
        form.Should().NotBeNull("page should contain a form to add users");
    }

    [Fact]
    public async Task Get_AvailableUsersExist_SelectContainsAvailableUsers()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        SeedUser("avail-user2", "avail2@test.com");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var select = document.QuerySelector("#add-user-form select[name='SelectedUserId']");
        select.Should().NotBeNull("form should have a user select dropdown");
        select!.TextContent.Should().Contain("avail-user2");
    }

    [Fact]
    public async Task Get_UserInRole_RowContainsRemoveForm()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("remove-me", "rm@test.com");
        AddUserToRole(userId, "Editor");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var removeForm = document.QuerySelector("#users-in-role-table form[id^='remove-user-form']");
        removeForm.Should().NotBeNull("each user row should contain a remove form");
    }

    // ── Step 4: POST add user success ────────────────────────────────────

    [Fact]
    public async Task PostAddUser_ValidUser_Returns302Redirect()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("add-me", "add@test.com");
        using var noRedirectClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await noRedirectClient.PostAsync(
            $"/Admin/Roles/{roleId}?handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = userId,
                ["RoleId"] = roleId
            }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain(roleId);
    }

    [Fact]
    public async Task PostAddUser_ValidUser_UserIsAddedToRole()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("newly-added", "new@test.com");

        // Act
        await _client.PostAsync(
            $"/Admin/Roles/{roleId}?handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = userId,
                ["RoleId"] = roleId
            }));

        // Assert — verify the user was actually added
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        var isInRole = await userManager.IsInRoleAsync(user!, "Editor");
        isInRole.Should().BeTrue();
    }

    // ── Step 5: POST remove user success ─────────────────────────────────

    [Fact]
    public async Task PostRemoveUser_ValidUser_Returns302Redirect()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("to-remove", "rem@test.com");
        AddUserToRole(userId, "Editor");
        using var noRedirectClient = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await noRedirectClient.PostAsync(
            $"/Admin/Roles/{roleId}?handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = userId,
                ["RoleId"] = roleId
            }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain(roleId);
    }

    [Fact]
    public async Task PostRemoveUser_ValidUser_UserIsRemovedFromRole()
    {
        // Arrange
        var roleId = SeedRole("Editor");
        var userId = SeedUser("will-remove", "wr@test.com");
        AddUserToRole(userId, "Editor");

        // Act
        await _client.PostAsync(
            $"/Admin/Roles/{roleId}?handler=RemoveUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = userId,
                ["RoleId"] = roleId
            }));

        // Assert
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        var isInRole = await userManager.IsInRoleAsync(user!, "Editor");
        isInRole.Should().BeFalse();
    }

    // ── Step 6: POST edge cases ──────────────────────────────────────────

    [Fact]
    public async Task PostAddUser_NoUserSelected_Returns200PageRerender()
    {
        // Arrange
        var roleId = SeedRole("Editor");

        // Act
        var response = await _client.PostAsync(
            $"/Admin/Roles/{roleId}?handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = "",
                ["RoleId"] = roleId
            }));

        // Assert — should re-render the page, not redirect
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostAddUser_RoleNotFound_Returns404()
    {
        // Act
        var response = await _client.PostAsync(
            "/Admin/Roles/nonexistent?handler=AddUser",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["SelectedUserId"] = "some-user",
                ["RoleId"] = "nonexistent"
            }));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Step 7: Roles Index contains Edit links ──────────────────────────

    [Fact]
    public async Task Get_RolesIndex_ContainsEditLinks()
    {
        // Arrange
        var roleId = SeedRole("TestRole");

        // Act
        var response = await _client.GetAsync("/Admin/Roles");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var editLink = document.QuerySelector($"a[href*='/Admin/Roles/{roleId}']");
        editLink.Should().NotBeNull("roles index should contain edit links for each role");
    }

    // ── Back to Roles link ───────────────────────────────────────────────

    [Fact]
    public async Task Get_RoleEditPage_ContainsBackToRolesLink()
    {
        // Arrange
        var roleId = SeedRole("TestRole2");

        // Act
        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        // Assert
        var backLink = document.QuerySelector("a[href*='/Admin/Roles']");
        backLink.Should().NotBeNull("edit page should contain a back link to roles list");
    }
}
