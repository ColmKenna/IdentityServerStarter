using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Claims;

[Trait("Category", "E2E")]
public class ClaimsAdminE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public ClaimsAdminE2eTests(PlaywrightFixture fixture)
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
    public async Task IndexPage_ShouldRenderClaimsAndNavigateToEdit()
    {
        var claimType = $"CustomClaim_{Guid.NewGuid():N}";
        var claimValue = "E2eTestValue";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "claim-idx-user");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, claimType, claimValue);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims");

        await Expect(_page.GetByText(claimType)).ToBeVisibleAsync();

        var manageLink = _page.Locator($"tr:has-text('{claimType}')").GetByRole(AriaRole.Link, new() { Name = "Manage Users" });
        await manageLink.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/Claims/Edit\\?claimType={claimType}"));
        await Expect(_page.Locator("h2").First).ToContainTextAsync(claimType);
    }

    [Fact]
    public async Task EditPage_ShouldSuccessfullyAddUserToClaim()
    {
        var claimType = $"AdminLevel_{Guid.NewGuid():N}";
        var existingUserId = await TestDataHelper.SeedUserAsync(_fixture.Factory, "existing-claim-user");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, existingUserId, claimType, "Read");

        var targetUsername = $"target-add-user-{Guid.NewGuid():N}";
        var targetUserId = await TestDataHelper.SeedUserAsync(_fixture.Factory, targetUsername);
        var newClaimValue = "Write";

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");

        await _page.Locator("select#SelectedUserId").SelectOptionAsync([targetUserId]);
        await _page.Locator("input#NewClaimValue").FillAsync(newClaimValue);

        var addButton = _page.GetByRole(AriaRole.Button, new() { Name = "Add User" });
        await addButton.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/Claims/Edit\\?claimType={claimType}"));
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("assigned to user");

        var tableRow = _page.Locator($"tr:has-text('{targetUsername}')");
        await Expect(tableRow).ToBeVisibleAsync();
        await Expect(tableRow.Locator("td").Nth(2)).ToHaveTextAsync(newClaimValue);
    }

    [Fact]
    public async Task EditPage_ShouldSuccessfullyRemoveUserFromClaim()
    {
        var claimType = $"Department_{Guid.NewGuid():N}";

        var user1Username = $"remove-target1-{Guid.NewGuid():N}";
        var user1Id = await TestDataHelper.SeedUserAsync(_fixture.Factory, user1Username);
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, user1Id, claimType, "HR");

        var user2Id = await TestDataHelper.SeedUserAsync(_fixture.Factory, $"remove-target2-{Guid.NewGuid():N}");
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, user2Id, claimType, "IT");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");

        var user1Row = _page.Locator($"tr:has-text('{user1Username}')");
        await Expect(user1Row).ToBeVisibleAsync();

        var removeButton = user1Row.GetByRole(AriaRole.Button, new() { Name = "Remove" });
        await removeButton.ClickAsync();

        var confirmButton = _page.Locator("#confirm-modal-confirm");
        await confirmButton.ClickAsync();

        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("removed from user");
        await Expect(_page.Locator($"tr:has-text('{user1Username}')")).ToBeHiddenAsync();

        var user2Row = _page.Locator($"tr:has-text('{user2Id}')");
    }

    [Fact]
    public async Task EditPage_ShouldRedirectToIndexWithWarning_WhenLastUserRemovedFromClaim()
    {
        var claimType = $"Exclusive_{Guid.NewGuid():N}";
        var targetUsername = $"last-user-{Guid.NewGuid():N}";
        var userId = await TestDataHelper.SeedUserAsync(_fixture.Factory, targetUsername);
        await TestDataHelper.AddUserClaimAsync(_fixture.Factory, userId, claimType, "OnlyMe");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Claims/Edit?claimType={claimType}");

        var row = _page.Locator($"tr:has-text('{targetUsername}')");

        var removeButton = row.GetByRole(AriaRole.Button, new() { Name = "Remove" });
        await removeButton.ClickAsync();

        var confirmButton = _page.Locator("#confirm-modal-confirm");
        await confirmButton.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Admin/Claims$"));
        await Expect(_page.Locator(".alert-warning")).ToContainTextAsync($"Claim type '{claimType}' no longer has any assigned users");
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
