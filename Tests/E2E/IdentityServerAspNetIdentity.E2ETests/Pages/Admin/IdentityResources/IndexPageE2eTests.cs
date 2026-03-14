using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.IdentityResources;

[Trait("Category", "E2E")]
public class IndexPageE2eTests : IClassFixture<PlaywrightFixture>, IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    private IBrowserContext _context = default!;
    private IPage _page = default!;

    public IndexPageE2eTests(PlaywrightFixture fixture)
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
    public async Task IndexPage_ShouldNavigateToEditPage_WhenEditClicked()
    {
        var resourceId = await TestDataHelper.SeedIdentityResourceAsync(
            _fixture.Factory, $"e2e-resource-{Guid.NewGuid():N}", displayName: "E2E Test Resource");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/IdentityResources");

        var row = _page.Locator("tr:has-text('E2E Test Resource')");
        await Expect(row).ToBeVisibleAsync();

        var editLink = row.GetByRole(AriaRole.Link, new() { Name = "Edit" });
        await editLink.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/IdentityResources/{resourceId}/Edit"));
        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit Identity Resource");
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
