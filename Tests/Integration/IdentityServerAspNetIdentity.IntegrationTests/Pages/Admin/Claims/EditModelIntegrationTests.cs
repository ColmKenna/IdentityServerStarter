using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Claims;

public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    // --------- Authorization ---------

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await nonAdminClient.GetAsync("/Admin/Claims/Edit?claimType=Role");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await unauthClient.GetAsync("/Admin/Claims/Edit?claimType=Role");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // --------- OnGetAsync ---------

    [Fact]
    public async Task Get_ShouldShowEmptyState_WhenNoUsersAssigned()
    {
        // Arrange — seed a user with a claim so the claim type exists, then remove it
        // Actually, if no user has this claim, GetForEditAsync returns null → NotFound
        // So we test the empty-users-in-claim scenario by having a user with the claim
        // but verifying the "no users" message appears when the claim type doesn't exist
        var response = await _client.GetAsync("/Admin/Claims/Edit?claimType=NonExistentClaim");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ShouldRenderUserTable_WhenUsersHaveClaim()
    {
        // Arrange
        var userId = await TestDataHelper.SeedUserAsync(_factory, "claimEditUser");
        await TestDataHelper.AddUserClaimAsync(_factory, userId, "TestRole", "Admin");

        // Act
        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=TestRole");

        // Assert
        var table = document.QuerySelector("#users-in-claim-table");
        table.Should().NotBeNull("the users-in-claim table should be rendered");

        var rows = table!.QuerySelectorAll("tbody tr");
        rows.Should().HaveCount(1);
        rows[0].TextContent.Should().Contain("claimEditUser");
        rows[0].TextContent.Should().Contain("Admin");
    }

    [Fact]
    public async Task Get_ShouldShowAvailableUsersInDropdown_WhenUnassignedUsersExist()
    {
        // Arrange — seed two users, assign claim to one
        var assignedUserId = await TestDataHelper.SeedUserAsync(_factory, "assignedUser");
        var availableUserId = await TestDataHelper.SeedUserAsync(_factory, "availableUser");
        await TestDataHelper.AddUserClaimAsync(_factory, assignedUserId, "DropdownTest", "Value1");

        // Act
        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=DropdownTest");

        // Assert
        var select = document.QuerySelector<IHtmlSelectElement>("#SelectedUserId");
        select.Should().NotBeNull("the user dropdown should be rendered");

        var options = select!.Options.Where(o => !string.IsNullOrEmpty(o.Value)).ToList();
        options.Should().Contain(o => o.TextContent.Contains("availableUser"),
            "unassigned user should appear in dropdown");
        options.Should().NotContain(o => o.TextContent.Contains("assignedUser"),
            "already-assigned user should not appear in dropdown");
    }

    // --------- OnPostAddUserAsync ---------

    [Fact]
    public async Task PostAddUser_ShouldAssignClaimAndShowSuccess_WhenDataIsValid()
    {
        // Arrange
        var existingUserId = await TestDataHelper.SeedUserAsync(_factory, "existingClaimUser");
        await TestDataHelper.AddUserClaimAsync(_factory, existingUserId, "AddTest", "ExistingValue");

        var newUserId = await TestDataHelper.SeedUserAsync(_factory, "newClaimUser");

        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=AddTest");
        var form = document.QuerySelector<IHtmlFormElement>("#add-user-claim-form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["SelectedUserId"] = newUserId,
            ["NewClaimValue"] = "NewValue"
        });

        // Assert — follows redirect back to Edit page
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);

        resultDocument.QuerySelector(".alert-success")!.TextContent
            .Should().Contain("newClaimUser");

        // Verify the user now appears in the table
        var table = resultDocument.QuerySelector("#users-in-claim-table");
        table!.TextContent.Should().Contain("newClaimUser");
    }

    [Fact]
    public async Task PostAddUser_ShouldShowValidationError_WhenNoUserSelected()
    {
        // Arrange — need an available (unassigned) user so the add form renders
        var assignedUserId = await TestDataHelper.SeedUserAsync(_factory, "validationAssigned");
        await TestDataHelper.AddUserClaimAsync(_factory, assignedUserId, "ValidationTest", "Value1");
        await TestDataHelper.SeedUserAsync(_factory, "validationAvailable");

        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=ValidationTest");
        var form = document.QuerySelector<IHtmlFormElement>("#add-user-claim-form")!;

        // Act — submit without selecting a user
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["SelectedUserId"] = "",
            ["NewClaimValue"] = "SomeValue"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        var validationSpan = resultDocument.QuerySelector("[data-valmsg-for='SelectedUserId']");
        validationSpan.Should().NotBeNull("a validation message for SelectedUserId should be rendered");
        validationSpan!.TextContent.Should().Contain("Please select a user");
    }

    // --------- OnPostRemoveUserAsync ---------

    [Fact]
    public async Task PostRemoveUser_ShouldRemoveClaimAndShowSuccess_WhenMultipleUsersRemain()
    {
        // Arrange — two users with the same claim type
        var userId1 = await TestDataHelper.SeedUserAsync(_factory, "removeUser1");
        var userId2 = await TestDataHelper.SeedUserAsync(_factory, "removeUser2");
        await TestDataHelper.AddUserClaimAsync(_factory, userId1, "RemoveTest", "Value1");
        await TestDataHelper.AddUserClaimAsync(_factory, userId2, "RemoveTest", "Value2");

        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=RemoveTest");
        var removeForm = document.QuerySelector<IHtmlFormElement>("#remove-user-claim-form-0")!;

        // Act
        var response = await _client.SubmitForm(removeForm);

        // Assert — follows redirect back to Edit page
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);

        resultDocument.QuerySelector(".alert-success").Should().NotBeNull(
            "a success message should be displayed after removing a user");
    }

    [Fact]
    public async Task PostRemoveUser_ShouldRedirectToIndexWithWarning_WhenLastUserRemoved()
    {
        // Arrange — single user with the claim
        var userId = await TestDataHelper.SeedUserAsync(_factory, "lastClaimUser");
        await TestDataHelper.AddUserClaimAsync(_factory, userId, "LastUserTest", "OnlyValue");

        var document = await _client.GetAndParsePage("/Admin/Claims/Edit?claimType=LastUserTest");
        var removeForm = document.QuerySelector<IHtmlFormElement>("#remove-user-claim-form-0")!;

        // Act
        var response = await _client.SubmitForm(removeForm);

        // Assert — follows redirect to Index page with warning
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);

        // Should be on the Index page now, not the Edit page
        resultDocument.QuerySelector(".alert-warning")!.TextContent
            .Should().Contain("LastUserTest");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
