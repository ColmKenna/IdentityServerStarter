using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Roles;

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
    public async Task OnPostAddUserAsync_ShouldAddUserAndRefreshMembership_WhenSubmittedFromBrowser()
    {
        var roleName = $"administrators-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_fixture.Factory, roleName);
        var existingMember = await CreateUserAsync("member");
        var userToAdd = await CreateUserAsync("candidate-add");
        var remainingAvailableUser = await CreateUserAsync("candidate-still-available");

        await TestDataHelper.AddUserToRoleAsync(_fixture.Factory, existingMember.Id, roleName);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Roles/{roleId}");
        await _page.Locator("select#SelectedUserId").SelectOptionAsync([userToAdd.Id]);

        await _page.RunAndWaitForNavigationAsync(async () =>
        {
            await _page.GetByRole(AriaRole.Button, new() { Name = "Add" }).ClickAsync();
        });

        _page.Url.Should().EndWith($"/Admin/Roles/{roleId}");
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("added to role");
        await Expect(_page.Locator($"#users-in-role-table tbody tr:has-text('{userToAdd.UserName}')")).ToBeVisibleAsync();

        var optionTexts = await GetAvailableUserOptionTextsAsync();
        optionTexts.Should().Contain(x => x.Contains(remainingAvailableUser.UserName, StringComparison.Ordinal));
        optionTexts.Should().NotContain(x => x.Contains(userToAdd.UserName, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnPostRemoveUserAsync_ShouldRemoveUserAndMakeThemSelectable_WhenSubmittedFromBrowser()
    {
        var roleName = $"operators-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_fixture.Factory, roleName);
        var userToRemove = await CreateUserAsync("member-remove");
        var remainingMember = await CreateUserAsync("member-stay");

        await TestDataHelper.AddUserToRoleAsync(_fixture.Factory, userToRemove.Id, roleName);
        await TestDataHelper.AddUserToRoleAsync(_fixture.Factory, remainingMember.Id, roleName);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Roles/{roleId}");

        var row = _page.Locator($"#users-in-role-table tbody tr:has-text('{userToRemove.UserName}')");

        await _page.RunAndWaitForNavigationAsync(async () =>
        {
            await row.GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        });

        _page.Url.Should().EndWith($"/Admin/Roles/{roleId}");
        await Expect(_page.Locator(".alert-success")).ToContainTextAsync("removed from role");
        await Expect(_page.Locator($"#users-in-role-table tbody tr:has-text('{userToRemove.UserName}')")).ToBeHiddenAsync();
        await Expect(_page.Locator($"#users-in-role-table tbody tr:has-text('{remainingMember.UserName}')")).ToBeVisibleAsync();
        await Expect(_page.Locator("select#SelectedUserId")).ToBeVisibleAsync();

        var optionTexts = await GetAvailableUserOptionTextsAsync();
        optionTexts.Should().Contain(x => x.Contains(userToRemove.UserName, StringComparison.Ordinal));
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

    private async Task<(string Id, string UserName, string Email)> CreateUserAsync(string prefix)
    {
        var userName = $"{prefix}-{Guid.NewGuid():N}";
        var email = $"{userName}@test.local";
        var id = await TestDataHelper.CreateTestUserAsync(_fixture.Factory, userName, email);
        return (id, userName, email);
    }

    private async Task<IReadOnlyList<string>> GetAvailableUserOptionTextsAsync()
    {
        return (await _page.Locator("#SelectedUserId option").AllTextContentsAsync())
            .Select(x => x.Trim())
            .ToList();
    }

    private ILocatorAssertions Expect(ILocator locator)
    {
        return Microsoft.Playwright.Assertions.Expect(locator);
    }
}
