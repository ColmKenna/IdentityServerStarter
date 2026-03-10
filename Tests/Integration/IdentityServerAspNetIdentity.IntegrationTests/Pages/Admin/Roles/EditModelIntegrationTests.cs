using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Roles;

public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenRoleDoesNotExist()
    {
        var response = await _client.GetAsync("/Admin/Roles/missing-role");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldRenderUsersInRoleAndAvailableUsers_WhenRoleHasMixedMembership()
    {
        var roleName = $"administrators-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_factory, roleName);
        var memberOne = await CreateUserAsync("member-one");
        var memberTwo = await CreateUserAsync("member-two");
        var availableUser = await CreateUserAsync("available-user");

        await TestDataHelper.AddUserToRoleAsync(_factory, memberOne.Id, roleName);
        await TestDataHelper.AddUserToRoleAsync(_factory, memberTwo.Id, roleName);

        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("h2")!.TextContent.Should().Contain(roleName);
        document.QuerySelector("#add-user-form").Should().NotBeNull();
        document.QuerySelector("#users-in-role-table").Should().NotBeNull();
        document.QuerySelector($"#SelectedUserId option[value='{availableUser.Id}']").Should().NotBeNull();
        document.QuerySelector($"#SelectedUserId option[value='{memberOne.Id}']").Should().BeNull();
        document.QuerySelector($"#SelectedUserId option[value='{memberTwo.Id}']").Should().BeNull();

        var rows = document.QuerySelectorAll("#users-in-role-table tbody tr");
        rows.Should().HaveCount(2);
        rows.Select(x => x.TextContent)
            .Should()
            .Contain(x => x.Contains(memberOne.UserName, StringComparison.Ordinal))
            .And.Contain(x => x.Contains(memberTwo.UserName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Get_ShouldRenderAllUsersAlreadyInRoleMessage_WhenNoAvailableUsersExist()
    {
        var roleName = $"operators-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_factory, roleName);
        var memberOne = await CreateUserAsync("member-one");
        var memberTwo = await CreateUserAsync("member-two");

        await TestDataHelper.AddUserToRoleAsync(_factory, memberOne.Id, roleName);
        await TestDataHelper.AddUserToRoleAsync(_factory, memberTwo.Id, roleName);

        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("#add-user-form").Should().BeNull();
        document.Body!.TextContent.Should().Contain("All users are already in this role.");
        document.QuerySelectorAll("#users-in-role-table tbody tr").Should().HaveCount(2);
    }

    [Fact]
    public async Task Get_ShouldRenderNoUsersMessage_WhenRoleHasNoAssignedUsers()
    {
        var roleName = $"auditors-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_factory, roleName);
        await CreateUserAsync("available-user");

        var response = await _client.GetAsync($"/Admin/Roles/{roleId}");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("#add-user-form").Should().NotBeNull();
        document.QuerySelector("#users-in-role-table").Should().BeNull();
        document.QuerySelector("#no-users-message")!.TextContent.Should().Contain("No users are currently in this role.");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<(string Id, string UserName, string Email)> CreateUserAsync(string prefix)
    {
        var userName = $"{prefix}-{Guid.NewGuid():N}";
        var email = $"{userName}@test.local";
        var id = await TestDataHelper.CreateTestUserAsync(_factory, userName, email);
        return (id, userName, email);
    }
}
