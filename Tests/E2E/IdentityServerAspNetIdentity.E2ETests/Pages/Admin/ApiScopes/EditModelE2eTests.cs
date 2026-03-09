using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.ApiScopes;

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
    public async Task OnPostAsync_ShouldCreateScopeAndReturnToEditPage_WhenSubmittedFromBrowser()
    {
        var scopeName = $"api-scope-{Guid.NewGuid():N}";
        var displayName = "Orders Read";
        var description = "Allows reading orders";

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/0/Edit");
        await GetField("Name").FillAsync(scopeName);
        await GetField("Display Name").FillAsync(displayName);
        await GetField("Description").FillAsync(description);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Create" }).ClickAsync();
        await _page.WaitForURLAsync("**/Admin/ApiScopes/*/Edit");

        _page.Url.Should().MatchRegex(@".*/Admin/ApiScopes/\d+/Edit$");
        (await _page.Locator(".alert-success").TextContentAsync()).Should().Contain("API scope created successfully");
        (await GetField("Name").InputValueAsync()).Should().Be(scopeName);
        (await GetField("Display Name").InputValueAsync()).Should().Be(displayName);
        (await GetField("Description").InputValueAsync()).Should().Be(description);
    }

    [Fact]
    public async Task OnPostAsync_ShouldPersistBasicInfoChanges_WhenEditingFromBrowser()
    {
        var scopeId = await TestDataHelper.SeedApiScopeAsync(
            _fixture.Factory,
            $"api-scope-{Guid.NewGuid():N}",
            displayName: "Original Display",
            description: "Original description",
            enabled: true);

        var updatedName = $"api-scope-updated-{Guid.NewGuid():N}";
        var updatedDisplayName = "Updated Display";
        var updatedDescription = "Updated description";

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/ApiScopes/{scopeId}/Edit");
        await GetField("Name").FillAsync(updatedName);
        await GetField("Display Name").FillAsync(updatedDisplayName);
        await GetField("Description").FillAsync(updatedDescription);
        await _page.RunAndWaitForNavigationAsync(async () =>
        {
            await _page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();
        });

        _page.Url.Should().EndWith($"/Admin/ApiScopes/{scopeId}/Edit");
        (await _page.Locator(".alert-success").TextContentAsync()).Should().Contain("API scope updated successfully");
        (await GetField("Name").InputValueAsync()).Should().Be(updatedName);
        (await GetField("Display Name").InputValueAsync()).Should().Be(updatedDisplayName);
        (await GetField("Description").InputValueAsync()).Should().Be(updatedDescription);
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

    private ILocator GetField(string label)
    {
        return _page.GetByLabel(label, new PageGetByLabelOptions
        {
            Exact = true
        });
    }
}
