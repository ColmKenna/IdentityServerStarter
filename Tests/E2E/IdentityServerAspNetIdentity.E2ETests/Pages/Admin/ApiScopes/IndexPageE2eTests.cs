using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.ApiScopes;

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
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory, $"e2e-scope-{Guid.NewGuid():N}", displayName: "E2E Test Scope");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes");

        var row = _page.Locator("tr:has-text('E2E Test Scope')");
        await Expect(row).ToBeVisibleAsync();

        var editLink = row.GetByRole(AriaRole.Link, new() { Name = "Edit" });
        await editLink.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex($"/Admin/ApiScopes/{scopeId}/Edit"));
        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit API Scope");
    }

    [Fact]
    public async Task IndexPage_ShouldNavigateToCreatePage_WhenCreateClicked()
    {
        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes");

        var createButton = _page.GetByRole(AriaRole.Link, new() { Name = "Create API Scope" });
        await Expect(createButton).ToBeVisibleAsync();
        await createButton.ClickAsync();

        await Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/Admin/ApiScopes/0/Edit"));
        await Expect(_page.Locator("h2").First).ToContainTextAsync("Create API Scope");
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
