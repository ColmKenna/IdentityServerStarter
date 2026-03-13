using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.ApiScopes;

[Trait("Category", "E2E")]
public class EditPageE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public EditPageE2eTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.CloseAsync();
    }

    // ── Create Mode ──

    [Fact]
    public async Task CreatePage_ShouldDisplayCreateHeading_AndEmptyForm()
    {
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/0/Edit");

        await Expect(_page.Locator("h2").First).ToContainTextAsync("Create API Scope");

        var nameInput = _page.Locator("#Input_Name");
        var displayNameInput = _page.Locator("#Input_DisplayName");
        var descriptionInput = _page.Locator("#Input_Description");

        await Expect(nameInput).ToHaveValueAsync("");
        await Expect(displayNameInput).ToHaveValueAsync("");
        await Expect(descriptionInput).ToHaveValueAsync("");
    }

    [Fact]
    public async Task CreatePage_ShouldShowSaveBeforeClaimsMessage()
    {
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/0/Edit");

        // Click the User Claims tab to reveal its content
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        var message = _page.Locator("#save-before-user-claims-message");
        await Expect(message).ToBeVisibleAsync();
        await Expect(message).ToContainTextAsync("Save API scope before managing applied user claims");
    }

    [Fact]
    public async Task CreatePage_ShouldCreateScopeAndRedirectToEditPage()
    {
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/0/Edit");

        var uniqueName = $"e2e-create-{Guid.NewGuid():N}";
        await _page.FillAsync("#Input_Name", uniqueName);
        await _page.FillAsync("#Input_DisplayName", "E2E Created Scope");
        await _page.FillAsync("#Input_Description", "Created via E2E test");

        await _page.Locator("#edit-api-scope-form button[type='submit']").ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/Admin/ApiScopes/\d+/Edit"));
        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit API Scope");
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("API scope created successfully");
    }

    // ── Edit Mode ──

    [Fact]
    public async Task EditPage_ShouldDisplayExistingScopeData()
    {
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-edit-{Guid.NewGuid():N}",
            displayName: "E2E Edit Scope",
            description: "E2E edit description",
            enabled: true);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");

        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit API Scope");
        await Expect(_page.Locator("#Input_DisplayName")).ToHaveValueAsync("E2E Edit Scope");
        await Expect(_page.Locator("#Input_Description")).ToHaveValueAsync("E2E edit description");
    }

    [Fact]
    public async Task EditPage_ShouldUpdateScopeAndShowSuccessMessage()
    {
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-update-{Guid.NewGuid():N}",
            displayName: "Original Name");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");

        await _page.FillAsync("#Input_DisplayName", "Updated Name");

        await _page.Locator("#edit-api-scope-form button[type='submit']").ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/ApiScopes/{scopeId}/Edit"));
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("API scope updated successfully");
        await Expect(_page.Locator("#Input_DisplayName")).ToHaveValueAsync("Updated Name");
    }

    // ── User Claims ──

    [Fact]
    public async Task EditPage_ShouldDisplayAppliedUserClaims()
    {
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-claims-{Guid.NewGuid():N}",
            displayName: "Claims Scope",
            userClaims: new[] { "sub", "email" });

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");

        // Click the User Claims tab
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        var claimsTable = _page.Locator("#applied-user-claims-table");
        await Expect(claimsTable).ToBeVisibleAsync();
        await Expect(claimsTable).ToContainTextAsync("sub");
        await Expect(claimsTable).ToContainTextAsync("email");
    }

    [Fact]
    public async Task EditPage_ShouldAddClaimAndShowSuccessMessage()
    {
        // Seed a user with a claim so it appears as an available claim type
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "e2e-claimuser");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, "profile", "test-value");

        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-addclaim-{Guid.NewGuid():N}",
            displayName: "Add Claim Scope");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");

        // Click the User Claims tab
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        // Select a claim from the dropdown and submit
        var select = _page.Locator("#available-user-claims-select");
        await Expect(select).ToBeVisibleAsync();

        await select.SelectOptionAsync("profile");
        await _page.Locator("#add-user-claim-form button[type='submit']").ClickAsync();

        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("added successfully");
    }

    [Fact]
    public async Task EditPage_ShouldRemoveClaimAndShowSuccessMessage()
    {
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-removeclaim-{Guid.NewGuid():N}",
            displayName: "Remove Claim Scope",
            userClaims: new[] { "sub" });

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");

        // Click the User Claims tab
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        // Click the remove button on the first claim
        var removeButton = _page.Locator("#remove-claim-form-0 button[type='submit']");
        await Expect(removeButton).ToBeVisibleAsync();
        await removeButton.ClickAsync();

        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("removed successfully");
    }

    // ── Validation ──

    [Fact]
    public async Task CreatePage_ShouldShowValidationError_WhenNameIsEmpty()
    {
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/0/Edit");

        // Submit without filling name
        await _page.Locator("#edit-api-scope-form button[type='submit']").ClickAsync();

        // Should stay on create page with validation error
        await Expect(_page.Locator("h2").First).ToContainTextAsync("Create API Scope");
        await Expect(_page.Locator("span[data-valmsg-for='Input.Name'].field-validation-error")).ToBeVisibleAsync();
    }

    // ── Helpers ──

    private ILocatorAssertions Expect(ILocator locator)
    {
        return Microsoft.Playwright.Assertions.Expect(locator);
    }

    private IPageAssertions Expect(IPage page)
    {
        return Microsoft.Playwright.Assertions.Expect(page);
    }
}
