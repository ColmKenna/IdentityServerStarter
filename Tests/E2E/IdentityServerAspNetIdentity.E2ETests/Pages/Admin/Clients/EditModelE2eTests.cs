using IdentityServerAspNetIdentity.E2ETests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using Microsoft.Playwright;

namespace IdentityServerAspNetIdentity.E2ETests.Pages.Admin.Clients;

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
    public async Task EditPage_ShouldUpdateClientDetails_WhenSavingValidChanges()
    {
        var id = await ClientTestDataHelper.SeedClientAsync(
            _fixture.Factory,
            $"client-{Guid.NewGuid():N}",
            clientName: "Original Client",
            description: "Original description");

        await _page.GotoAsync($"{_fixture.RootUrl}/Admin/Clients/{id}/Edit");

        await Expect(_page.Locator("h2").First).ToContainTextAsync("Edit Client");

        var clientNameInput = _page.GetByLabel("Client Name");
        var descriptionInput = _page.GetByLabel("Description", new() { Exact = true });
        await Expect(clientNameInput).ToHaveValueAsync("Original Client");
        await Expect(descriptionInput).ToHaveValueAsync("Original description");

        await clientNameInput.FillAsync("Updated Client");
        await descriptionInput.FillAsync("Updated description");

        await _page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        await Expect(_page).ToHaveURLAsync(
            new System.Text.RegularExpressions.Regex(
                $"/admin/clients/{id}/edit$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await Expect(_page.Locator("[role='alert']")).ToContainTextAsync("Client updated successfully");
        await Expect(_page.GetByLabel("Client Name")).ToHaveValueAsync("Updated Client");
        await Expect(descriptionInput).ToHaveValueAsync("Updated description");
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
