using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;
using FluentAssertions;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Claims;

[Trait("Category", "E2E")]
public class EditModelE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public EditModelE2eTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task EditClaim_ShouldRenderClaimDetails_WhenNavigatingToPage()
    {
        // Assemble data
        var claimType = $"test-claim-{Guid.NewGuid():N}";
        var claimValue = "test-value";
        
        var username = $"claimuser-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, username, $"{username}@test.local");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, claimType, claimValue);

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");

        // Assert
        await Expect(_page.Locator("h2").First).ToHaveTextAsync($" Claim: {claimType}");
        
        // Let's verify our user is in the existing claims list table
        var userInTable = _page.Locator("#users-in-claim-table tbody tr").First;
        await Expect(userInTable).ToBeVisibleAsync();
        await Expect(userInTable.Locator("td").Nth(0)).ToContainTextAsync(username);
        await Expect(userInTable.Locator("td").Nth(2)).ToHaveTextAsync(claimValue);
    }

    [Fact]
    public async Task EditClaim_ShouldSuccessfullySubmitForm_WhenAddingUserToClaim()
    {
        // Assemble data
        var claimType = $"new-claim-{Guid.NewGuid():N}";
        
        // The ClaimsAdminService dictates that a claim "exists" only if at least one user has it.
        // We must seed an initial user with this claim to view the Edit page.
        var initialUsername = $"initialuser-{Guid.NewGuid():N}";
        var initialUserId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, initialUsername, $"{initialUsername}@test.local");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, initialUserId, claimType, "initial-value");

        // Need a target user to assign to via the UI
        var username = $"targetuser-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, username, $"{username}@test.local");
        
        var newClaimValue = "assigned-value";

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");
        await _page.Locator("#SelectedUserId").SelectOptionAsync(new[] { userId });
        await _page.Locator("#NewClaimValue").FillAsync(newClaimValue);
        
        await _page.RunAndWaitForNavigationAsync(async () =>
        {
            await _page.GetByRole(AriaRole.Button, new() { NameString = "Add User" }).ClickAsync();
        });

        // Assert
        _page.Url.Should().Contain($"/Admin/Claims/Edit?claimType={claimType}");
        
        var alert = _page.Locator(".alert-success");
        await Expect(alert).ToBeVisibleAsync();
        await Expect(alert).ToContainTextAsync($"Claim '{claimType}' assigned to user '{username}'.");
        
        // Verify they appear in the grid
        var userInTable = _page.Locator("#users-in-claim-table tbody tr").Filter(new() { HasText = username });
        await Expect(userInTable).ToBeVisibleAsync();
        await Expect(userInTable.Locator("td").Nth(0)).ToContainTextAsync(username);
        await Expect(userInTable.Locator("td").Nth(2)).ToHaveTextAsync(newClaimValue);
    }

    [Fact]
    public async Task EditClaim_ShouldShowValidationError_WhenNoUserSelected()
    {
        // Arrange
        var claimType = $"val-nouser-{Guid.NewGuid():N}";

        var existingUsername = $"existinguser-{Guid.NewGuid():N}";
        var existingUserId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, existingUsername, $"{existingUsername}@test.local");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, existingUserId, claimType, "some-value");

        // Need an available user so the add form renders
        var availableUsername = $"availableuser-{Guid.NewGuid():N}";
        await TestDataHelper.CreateTestUserAsync(_fixture.Factory, availableUsername, $"{availableUsername}@test.local");

        // Act — submit without selecting a user
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");
        await _page.Locator("#NewClaimValue").FillAsync("SomeValue");

        await _page.GetByRole(AriaRole.Button, new() { NameString = "Add User" }).ClickAsync();

        // Assert — validation error should be visible
        var validationMessage = _page.Locator("[data-valmsg-for='SelectedUserId']");
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("Please select a user");
    }

    [Fact]
    public async Task EditClaim_ShouldShowValidationError_WhenClaimValueIsEmpty()
    {
        // Arrange
        var claimType = $"val-novalue-{Guid.NewGuid():N}";

        var existingUsername = $"existinguser2-{Guid.NewGuid():N}";
        var existingUserId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, existingUsername, $"{existingUsername}@test.local");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, existingUserId, claimType, "some-value");

        var availableUsername = $"availableuser2-{Guid.NewGuid():N}";
        var availableUserId = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, availableUsername, $"{availableUsername}@test.local");

        // Act — select a user but leave claim value empty
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");
        await _page.Locator("#SelectedUserId").SelectOptionAsync(new[] { availableUserId });
        await _page.Locator("#NewClaimValue").FillAsync("");

        await _page.GetByRole(AriaRole.Button, new() { NameString = "Add User" }).ClickAsync();

        // Assert — validation error should be visible
        var validationMessage = _page.Locator("[data-valmsg-for='NewClaimValue']");
        await Expect(validationMessage).ToBeVisibleAsync();
        await Expect(validationMessage).ToContainTextAsync("Claim value is required");
    }

    public async Task InitializeAsync()
    {
        _context = await _fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null)
        {
            await _page.CloseAsync();
        }

        if (_context is not null)
        {
            await _context.DisposeAsync();
        }
    }

    private ILocatorAssertions Expect(ILocator locator) => Microsoft.Playwright.Assertions.Expect(locator);
}
