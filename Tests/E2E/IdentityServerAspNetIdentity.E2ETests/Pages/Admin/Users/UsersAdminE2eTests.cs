using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Users;

[Trait("Category", "E2E")]
public class UsersAdminE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public UsersAdminE2eTests(PlaywrightFixture fixture)
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

    /// <summary>
    /// Clicks an action button that triggers a confirmation dialog, then confirms.
    /// Many buttons on the Edit page use confirmAction() from confirmModal.js,
    /// which shows a dialog and only submits the form when the user clicks Confirm.
    /// </summary>
    private async Task ClickAndConfirmAsync(string buttonSelector)
    {
        await _page.Locator(buttonSelector).ClickAsync();
        await Expect(_page.Locator("#confirm-modal")).ToBeVisibleAsync();
        await _page.Locator("#confirm-modal-confirm").ClickAsync();
    }

    #region Index Page

    [Fact]
    public async Task IndexPage_ShouldDisplayUsersAndNavigateToEdit()
    {
        // Arrange
        var usernamePrefix = $"index-user-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);
        var user = await TestDataHelper.GetUserByIdAsync(_fixture.Factory, userId);
        var actualUsername = user!.UserName!;

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users");

        // Assert — user appears in the list
        var userLink = _page.GetByRole(AriaRole.Link, new() { Name = actualUsername });
        await Expect(userLink).ToBeVisibleAsync();

        // Navigate to Edit via the username link
        await userLink.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/Users/{userId}"));
    }

    #endregion

    #region Profile Tab

    [Fact]
    public async Task ProfileTab_ShouldDisplayPrePopulatedFields()
    {
        // Arrange
        var usernamePrefix = $"profile-view-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);
        var user = await TestDataHelper.GetUserByIdAsync(_fixture.Factory, userId);
        var actualUsername = user!.UserName!;
        var actualEmail = user.Email!;

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}");

        // Assert — Profile tab fields are pre-populated with correct values
        await Expect(_page.Locator("input[id='Input_Username']")).ToHaveValueAsync(actualUsername);
        await Expect(_page.Locator("input[id='Input_Email']")).ToHaveValueAsync(actualEmail);
    }

    [Fact]
    public async Task ProfileTab_ShouldUpdateFieldsAndShowSuccess()
    {
        // Arrange
        var usernamePrefix = $"profile-update-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);

        var newUsername = $"updated-{Guid.NewGuid():N}";
        var newEmail = $"updated-{Guid.NewGuid():N}@test.local";

        // Act
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}");

        await _page.Locator("input[id='Input_Username']").FillAsync(newUsername);
        await _page.Locator("input[id='Input_Email']").FillAsync(newEmail);

        // The Save button doesn't have formnovalidate, and the Security tab has a
        // required password field. Submit the form bypassing client-side validation.
        await _page.EvaluateAsync(@"() => {
            const form = document.getElementById('edit-user-page-form');
            if (form) {
                form.noValidate = true;
                form.requestSubmit();
            }
        }");

        // Assert — success message and updated values displayed
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("User updated successfully");
        await Expect(_page.Locator("input[id='Input_Username']")).ToHaveValueAsync(newUsername);
        await Expect(_page.Locator("input[id='Input_Email']")).ToHaveValueAsync(newEmail);
    }

    #endregion

    #region Claims Tab

    [Fact]
    public async Task ClaimsTab_ShouldAddClaimViaModalAndDisplayInList()
    {
        // Arrange
        var usernamePrefix = $"claims-add-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);
        var claimType = "test-claim";
        var claimValue = $"value-{Guid.NewGuid():N}";

        // Act — navigate to the Claims tab
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=claims");

        // Open the Add Claim modal
        await _page.Locator("#open-add-claim-modal-btn").ClickAsync();
        await Expect(_page.Locator("#add-claim-modal")).ToBeVisibleAsync();

        // Fill in claim details and submit
        await _page.Locator("#NewClaimType").FillAsync(claimType);
        await _page.Locator("#NewClaimValue").FillAsync(claimValue);
        await _page.Locator("#add-claim-btn").ClickAsync();

        // Assert — success message and claim appears in the claims list
        await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
        await Expect(_page.Locator("#claims-table")).ToContainTextAsync(claimType);
        await Expect(_page.Locator("#claims-table")).ToContainTextAsync(claimValue);
    }

    [Fact]
    public async Task ClaimsTab_ShouldRemoveSelectedClaimAndUpdateList()
    {
        // Arrange
        var usernamePrefix = $"claims-remove-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);
        var claimType = "removable-claim";
        var claimValue = $"remove-me-{Guid.NewGuid():N}";
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, claimType, claimValue);

        // Act — navigate to the Claims tab
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=claims");

        // Verify the claim is displayed
        await Expect(_page.Locator("#claims-table")).ToContainTextAsync(claimType);

        // Select the claim checkbox and click Remove Selected — triggers confirmation dialog
        await _page.Locator($".claim-checkbox[data-claim-type='{claimType}']").CheckAsync();
        await ClickAndConfirmAsync("#remove-selected-btn");

        // Assert — success message and claim no longer in list
        await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
        await Expect(_page.Locator("#no-claims-message")).ToBeVisibleAsync();
    }

    #endregion

    #region Roles Tab

    [Fact]
    public async Task RolesTab_ShouldAddRoleAndDisplayInCurrentRoles()
    {
        // Arrange
        var roleName = $"AddableRole_{Guid.NewGuid():N}";
        await TestDataHelper.SeedRoleAsync(_fixture.Factory, roleName);

        var usernamePrefix = $"roles-add-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);

        // Act — navigate to the Roles tab
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=roles");

        // Check the available role checkbox and click Add
        await _page.Locator($"input#role-{roleName}").CheckAsync();
        await _page.Locator("#add-roles-btn").ClickAsync();

        // Assert — success message and role appears in Current Roles section
        await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
        await Expect(_page.Locator("#assigned-roles-list")).ToContainTextAsync(roleName);
    }

    [Fact]
    public async Task RolesTab_ShouldRemoveRoleAndUpdateList()
    {
        // Arrange
        var roleName = $"RemovableRole_{Guid.NewGuid():N}";
        await TestDataHelper.SeedRoleAsync(_fixture.Factory, roleName);

        var usernamePrefix = $"roles-remove-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);
        await TestDataHelper.AddUserToRoleAsync(_fixture.Factory, userId, roleName);

        // Act — navigate to the Roles tab
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=roles");

        // Verify the role is displayed in Current Roles
        await Expect(_page.Locator("#assigned-roles-list")).ToContainTextAsync(roleName);

        // Select the role checkbox and click Remove Selected — triggers confirmation dialog
        await _page.Locator($".role-checkbox[data-role-name='{roleName}']").CheckAsync();
        await ClickAndConfirmAsync("#remove-roles-btn");

        // Assert — success message and role no longer in Current Roles
        await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
        await Expect(_page.Locator("#no-roles-message")).ToBeVisibleAsync();
    }

    #endregion

    #region Security Tab

    [Fact]
    public async Task SecurityTab_ShouldDisableAccountAndShowDisabledStatus()
    {
        // Arrange
        var usernamePrefix = $"security-disable-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);

        // Act — navigate to the Security tab
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=security");

        // Verify initial status is Active
        await Expect(_page.Locator("#account-status")).ToContainTextAsync("Active");

        // Click Disable Account — triggers confirmation dialog
        await ClickAndConfirmAsync("#disable-account-btn");

        // Assert — success message and account status shows Disabled
        await Expect(_page.Locator(".alert-success")).ToBeVisibleAsync();
        await Expect(_page.Locator("#account-status")).ToContainTextAsync("Disabled");
    }

    [Fact]
    public async Task SecurityTab_ShouldEnableDisabledAccountAndShowActiveStatus()
    {
        // Arrange — create user and disable them via the UI
        var usernamePrefix = $"security-enable-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, usernamePrefix);

        // Disable the account first
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Users/{userId}?tab=security");
        await ClickAndConfirmAsync("#disable-account-btn");
        await Expect(_page.Locator("#account-status")).ToContainTextAsync("Disabled");

        // Act — click Enable Account (no confirmation dialog on Enable)
        await _page.Locator("#enable-account-btn").ClickAsync();

        // Assert — account status shows Active
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("enabled");
        await Expect(_page.Locator("#account-status")).ToContainTextAsync("Active");
    }

    #endregion
}
