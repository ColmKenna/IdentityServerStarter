using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.IdentityResources;

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

    [Fact]
    public async Task EditPage_ShouldDisplayExistingResourceData()
    {
        var uniqueName = $"e2e-edit-{Guid.NewGuid():N}";
        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, uniqueName,
            displayName: "E2E Edit Resource",
            description: "E2E edit description",
            enabled: true);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources/{resourceId}/Edit");

        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit Identity Resource");
        await Expect(_page.Locator("#Input_Name")).ToHaveValueAsync(uniqueName);
        await Expect(_page.Locator("#Input_DisplayName")).ToHaveValueAsync("E2E Edit Resource");
        await Expect(_page.Locator("#Input_Description")).ToHaveValueAsync("E2E edit description");
    }

    [Fact]
    public async Task EditPage_ShouldUpdateResourceAndShowSuccessMessage()
    {
        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, $"e2e-update-{Guid.NewGuid():N}",
            displayName: "Original Name");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources/{resourceId}/Edit");

        await _page.FillAsync("#Input_DisplayName", "Updated Name");

        await _page.Locator("#edit-identity-resource-form button[type='submit']").ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/IdentityResources/{resourceId}/Edit"));
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("Identity resource updated successfully");
        await Expect(_page.Locator("#Input_DisplayName")).ToHaveValueAsync("Updated Name");
    }

    [Fact]
    public async Task EditPage_ShouldAddClaimAndShowSuccessMessage()
    {
        // Seed a user with a claim so it appears as an available claim type
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "e2e-claimuser-ir");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, "given_name", "test-value");

        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, $"e2e-addclaim-ir-{Guid.NewGuid():N}",
            displayName: "Add Claim Resource");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources/{resourceId}/Edit");

        // Click the User Claims tab
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        // Select a claim from the dropdown and submit
        var select = _page.Locator("#available-user-claims-select");
        await Expect(select).ToBeVisibleAsync();

        await select.SelectOptionAsync("given_name");
        await _page.Locator("#add-user-claim-form button[type='submit']").ClickAsync();

        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("added successfully");
    }

    [Fact]
    public async Task EditPage_ShouldRemoveClaimAndShowSuccessMessage()
    {
        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, $"e2e-removeclaim-ir-{Guid.NewGuid():N}",
            displayName: "Remove Claim Resource",
            userClaims: new[] { "family_name" });

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources/{resourceId}/Edit");

        // Click the User Claims tab
        await _page.GetByRole(AriaRole.Tab, new() { Name = "User Claims" }).ClickAsync();

        // Click the remove button on the first claim
        var removeButton = _page.Locator("#remove-claim-form-0 button[type='submit']");
        await Expect(removeButton).ToBeVisibleAsync();
        await removeButton.ClickAsync();

        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("removed successfully");
    }

    [Fact]
    public async Task EditPage_ShouldShowValidationError_WhenNameIsCleared()
    {
        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, $"e2e-validation-{Guid.NewGuid():N}");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources/{resourceId}/Edit");

        // Clear the required Name field
        await _page.FillAsync("#Input_Name", "");
        
        // Attempt to submit
        await _page.Locator("#edit-identity-resource-form button[type='submit']").ClickAsync();

        // Target the specific validation span to avoid strict mode violations (Testing Guide Pitfall #14)
        var validationSpan = _page.Locator("span[data-valmsg-for='Input.Name'].field-validation-error");
        await Expect(validationSpan).ToBeVisibleAsync();
        
        // Wait for it to have text (usually "The Name field is required." from DataAnnotations)
        await Expect(validationSpan).Not.ToBeEmptyAsync();
        
        // Ensure we are still on the edit page (no navigation occurred)
        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/IdentityResources/{resourceId}/Edit"));
    }

    private ILocatorAssertions Expect(ILocator locator)
    {
        return Microsoft.Playwright.Assertions.Expect(locator);
    }

    private IPageAssertions Expect(IPage page)
    {
        return Microsoft.Playwright.Assertions.Expect(page);
    }
}
