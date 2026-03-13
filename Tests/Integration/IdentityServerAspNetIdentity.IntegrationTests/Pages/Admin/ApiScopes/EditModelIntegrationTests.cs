using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.ApiScopes;

public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    // --- GET: Create Mode ---

    [Fact]
    public async Task Get_ShouldRenderCreateForm_WhenIdIsZero()
    {
        // Act
        var document = await _client.GetAndParsePage("/Admin/ApiScopes/0/Edit");

        // Assert
        document.QuerySelector("h2")!.TextContent.Should().Contain("Create API Scope");
        document.QuerySelector("#edit-api-scope-form").Should().NotBeNull();
        document.QuerySelector("#edit-api-scope-form button[type='submit']")!
            .TextContent.Should().Contain("Create");
        document.QuerySelector("#save-before-user-claims-message").Should().NotBeNull();
    }

    // --- GET: Edit Mode ---

    [Fact]
    public async Task Get_ShouldRenderEditFormWithScopeData_WhenScopeExists()
    {
        // Arrange
        var id = await TestDataHelper.SeedApiScopeAsync(
            _factory, "test-scope",
            displayName: "Test Scope",
            description: "A test scope",
            enabled: true,
            userClaims: ["sub", "email"]);

        // Act
        var document = await _client.GetAndParsePage($"/Admin/ApiScopes/{id}/Edit");

        // Assert
        document.QuerySelector("h2")!.TextContent.Should().Contain("Edit API Scope");

        var nameInput = document.QuerySelector<IHtmlInputElement>("#Input_Name");
        nameInput!.Value.Should().Be("test-scope");

        var displayNameInput = document.QuerySelector<IHtmlInputElement>("#Input_DisplayName");
        displayNameInput!.Value.Should().Be("Test Scope");

        var descriptionTextarea = document.QuerySelector<IHtmlTextAreaElement>("#Input_Description");
        descriptionTextarea!.Value.Should().Be("A test scope");

        document.QuerySelector("#edit-api-scope-form button[type='submit']")!
            .TextContent.Should().Contain("Save Changes");
    }

    [Fact]
    public async Task Get_ShouldRenderAppliedUserClaims_WhenScopeHasClaims()
    {
        // Arrange
        var id = await TestDataHelper.SeedApiScopeAsync(
            _factory, "scope-with-claims",
            userClaims: ["sub", "email"]);

        // Act
        var document = await _client.GetAndParsePage($"/Admin/ApiScopes/{id}/Edit");

        // Assert
        var claimsTable = document.QuerySelector("#applied-user-claims-table");
        claimsTable.Should().NotBeNull();

        var rows = claimsTable!.QuerySelectorAll("tbody tr");
        rows.Should().HaveCount(2);
        rows.Should().Contain(r => r.TextContent.Contains("sub"));
        rows.Should().Contain(r => r.TextContent.Contains("email"));
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/Admin/ApiScopes/99999/Edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- POST: Create ---

    [Fact]
    public async Task Post_ShouldCreateScopeAndRedirect_WhenDataIsValid()
    {
        // Arrange
        var document = await _client.GetAndParsePage("/Admin/ApiScopes/0/Edit");
        var form = document.QuerySelector<IHtmlFormElement>("#edit-api-scope-form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["Input.Name"] = "new-integration-scope",
            ["Input.DisplayName"] = "New Integration Scope",
            ["Input.Description"] = "Created via integration test",
            ["Input.Enabled"] = "true"
        });

        // Assert — client follows redirect, so we land on the edit page
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        resultDocument.QuerySelector("h2")!.TextContent.Should().Contain("Edit API Scope");

        var nameInput = resultDocument.QuerySelector<IHtmlInputElement>("#Input_Name");
        nameInput!.Value.Should().Be("new-integration-scope");
    }

    // --- POST: Edit/Update ---

    [Fact]
    public async Task Post_ShouldUpdateScopeAndRedirect_WhenEditDataIsValid()
    {
        // Arrange
        var id = await TestDataHelper.SeedApiScopeAsync(
            _factory, "scope-to-update",
            displayName: "Original Display",
            description: "Original Description",
            enabled: true);

        var document = await _client.GetAndParsePage($"/Admin/ApiScopes/{id}/Edit");
        var form = document.QuerySelector<IHtmlFormElement>("#edit-api-scope-form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["Input.Name"] = "updated-scope-name",
            ["Input.DisplayName"] = "Updated Display",
            ["Input.Description"] = "Updated Description",
            ["Input.Enabled"] = "true"
        });

        // Assert — follows redirect back to the edit page
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);

        var nameInput = resultDocument.QuerySelector<IHtmlInputElement>("#Input_Name");
        nameInput!.Value.Should().Be("updated-scope-name");

        var displayNameInput = resultDocument.QuerySelector<IHtmlInputElement>("#Input_DisplayName");
        displayNameInput!.Value.Should().Be("Updated Display");
    }

    [Fact]
    public async Task Post_ShouldShowDuplicateNameError_WhenNameAlreadyExists()
    {
        // Arrange — seed two scopes, then try to rename second to match first
        await TestDataHelper.SeedApiScopeAsync(_factory, "existing-name");
        var id = await TestDataHelper.SeedApiScopeAsync(_factory, "scope-to-rename");

        var document = await _client.GetAndParsePage($"/Admin/ApiScopes/{id}/Edit");
        var form = document.QuerySelector<IHtmlFormElement>("#edit-api-scope-form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["Input.Name"] = "existing-name",
            ["Input.DisplayName"] = "Whatever",
            ["Input.Description"] = "Whatever",
            ["Input.Enabled"] = "true"
        });

        // Assert — stays on the page with an error
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);
        resultDocument.QuerySelector(".alert-danger")!.TextContent
            .Should().Contain("An API scope with this name already exists.");
    }

    // --- Authorization ---

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await nonAdminClient.GetAsync("/Admin/ApiScopes/0/Edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await unauthClient.GetAsync("/Admin/ApiScopes/0/Edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
