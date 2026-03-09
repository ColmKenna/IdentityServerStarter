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

    [Fact]
    public async Task OnPostAsync_ShouldPersistBasicInfoAndSelections_WhenSubmittedFromBrowser()
    {
        var identityScope = $"openid-{Guid.NewGuid():N}";
        var apiScope = $"api-{Guid.NewGuid():N}";

        await ClientTestDataHelper.SeedIdentityResourcesAsync(_fixture.Factory, identityScope);
        await ClientTestDataHelper.SeedApiScopesAsync(_fixture.Factory, apiScope);

        var clientId = await ClientTestDataHelper.SeedClientAsync(
            _fixture.Factory,
            clientId: $"interactive-{Guid.NewGuid():N}",
            clientName: "Original Client",
            description: "Original description",
            allowedGrantTypes: ["authorization_code"],
            redirectUris: ["https://client.test/callback"],
            postLogoutRedirectUris: ["https://client.test/logout"],
            allowedScopes: [identityScope]);

        await _page.GotoAsync($"{_fixture.RootUrl}/admin/clients/{clientId}/edit");
        await GetTextField("Client Name").FillAsync("Updated Client");
        await GetTextField("Description").FillAsync("Updated description");
        await OpenTabAsync("Allowed Grant Types");
        await GetCheckboxLabel("grant-types-fieldset", "client_credentials").ClickAsync();
        await OpenTabAsync("Scopes");
        await GetCheckboxLabel("scopes-fieldset", apiScope).ClickAsync();
        await Task.WhenAll(
            _page.WaitForURLAsync($"**/admin/clients/{clientId}/edit"),
            _page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync());

        _page.Url.Should().EndWith($"/admin/clients/{clientId}/edit");
        (await _page.Locator(".alert-success").TextContentAsync()).Should().Contain("Client updated successfully");
        (await GetTextField("Client Name").InputValueAsync()).Should().Be("Updated Client");
        (await GetTextField("Description").InputValueAsync()).Should().Be("Updated description");
        (await GetCheckbox("grant-types-fieldset", "client_credentials").IsCheckedAsync()).Should().BeTrue();
        (await GetCheckbox("scopes-fieldset", apiScope).IsCheckedAsync()).Should().BeTrue();
    }

    [Fact]
    public async Task OnGetAsync_ShouldDisplayExistingUris_WhenPageInitializesUriEditors()
    {
        var identityScope = $"openid-{Guid.NewGuid():N}";
        var redirectUri = "https://client.test/callback";
        var postLogoutRedirectUri = "https://client.test/logout";

        await ClientTestDataHelper.SeedIdentityResourcesAsync(_fixture.Factory, identityScope);

        var clientId = await ClientTestDataHelper.SeedClientAsync(
            _fixture.Factory,
            clientId: $"interactive-{Guid.NewGuid():N}",
            clientName: "Original Client",
            description: "Original description",
            allowedGrantTypes: ["authorization_code"],
            redirectUris: [redirectUri],
            postLogoutRedirectUris: [postLogoutRedirectUri],
            allowedScopes: [identityScope]);

        await _page.GotoAsync($"{_fixture.RootUrl}/admin/clients/{clientId}/edit");
        await OpenTabAsync("URIs");

        var redirectUriValues = _page.Locator("#redirectUrisEditor .edit-array-item [data-display-for='value']");
        await redirectUriValues.First.WaitForAsync();

        (await redirectUriValues.AllTextContentsAsync()).Should().Contain(redirectUri);

        var postLogoutUriValues = _page.Locator("#postLogoutRedirectUrisEditor .edit-array-item [data-display-for='value']");
        await postLogoutUriValues.First.WaitForAsync();

        (await postLogoutUriValues.AllTextContentsAsync()).Should().Contain(postLogoutRedirectUri);
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

    private ILocator GetTextField(string label)
    {
        return _page.GetByLabel(label, new PageGetByLabelOptions
        {
            Exact = true
        });
    }

    private ILocator GetCheckbox(string fieldsetId, string value)
    {
        return _page.Locator($"#{fieldsetId} input[value='{value}']");
    }

    private ILocator GetCheckboxLabel(string fieldsetId, string value)
    {
        return _page.Locator($"#{fieldsetId} label:has(input[value='{value}'])");
    }

    private async Task OpenTabAsync(string label)
    {
        await _page.GetByRole(AriaRole.Tab, new PageGetByRoleOptions
        {
            Name = label,
            Exact = true
        }).ClickAsync();
    }
}
