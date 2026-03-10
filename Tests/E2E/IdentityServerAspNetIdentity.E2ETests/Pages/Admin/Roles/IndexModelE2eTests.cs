using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Roles;

[Trait("Category", "E2E")]
public class IndexModelE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public IndexModelE2eTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task IndexPage_ShouldNavigateToEditPage_WhenEditLinkIsClicked()
    {
        var roleName = $"role-{Guid.NewGuid():N}";
        var roleId = await TestDataHelper.SeedRoleAsync(_fixture.Factory, roleName);

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Roles");

        var editLink = _page.Locator($"a[href='/Admin/Roles/{roleId}']").First;
        var waitForUrlTask = _page.WaitForURLAsync($"**/Admin/Roles/{roleId}");

        await editLink.ClickAsync();
        await waitForUrlTask;

        _page.Url.Should().EndWith($"/Admin/Roles/{roleId}");
        await Expect(_page.Locator("h2").First).ToContainTextAsync(roleName);
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

    private ILocatorAssertions Expect(ILocator locator)
    {
        return Microsoft.Playwright.Assertions.Expect(locator);
    }
}
