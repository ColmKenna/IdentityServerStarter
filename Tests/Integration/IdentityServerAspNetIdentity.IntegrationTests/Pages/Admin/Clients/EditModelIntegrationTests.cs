using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Clients;

public class EditModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EditModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldRenderPersistedSelectionsAndUriEditors_WhenClientExists()
    {
        var identityScope = $"openid-{Guid.NewGuid():N}";
        var apiScope = $"api-{Guid.NewGuid():N}";
        var redirectUri = "https://client.test/callback";
        var postLogoutRedirectUri = "https://client.test/logout";
        var clientIdentifier = $"interactive-{Guid.NewGuid():N}";

        await ClientTestDataHelper.SeedIdentityResourcesAsync(_factory, identityScope);
        await ClientTestDataHelper.SeedApiScopesAsync(_factory, apiScope);

        var clientId = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            clientId: clientIdentifier,
            clientName: "Interactive Client",
            description: "Interactive description",
            allowedGrantTypes: ["authorization_code"],
            redirectUris: [redirectUri],
            postLogoutRedirectUris: [postLogoutRedirectUri],
            allowedScopes: [identityScope, apiScope],
            clientUri: "https://client.test",
            logoUri: "https://client.test/logo.png");

        var response = await _client.GetAsync($"/admin/clients/{clientId}/edit");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("h2")!.TextContent.Should().Contain("Edit Client");
        document.QuerySelector("input[name='Input.ClientId']")!.GetAttribute("value").Should().Be(clientIdentifier);
        document.QuerySelector("input[name='Input.ClientName']")!.GetAttribute("value").Should().Be("Interactive Client");
        document.QuerySelector("textarea[name='Input.Description']")!.TextContent.Should().Be("Interactive description");
        document.QuerySelectorAll("#grant-types-fieldset input[checked]")
            .Select(x => x.GetAttribute("value"))
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("authorization_code");
        document.QuerySelectorAll("#scopes-fieldset input[checked]")
            .Select(x => x.GetAttribute("value"))
            .Should()
            .Contain([identityScope, apiScope]);
        document.QuerySelector("#redirectUrisEditor")!.GetAttribute("data-initial-json").Should().Contain(redirectUri);
        document.QuerySelector("#postLogoutRedirectUrisEditor")!.GetAttribute("data-initial-json").Should().Contain(postLogoutRedirectUri);
    }

    [Fact]
    public async Task Post_ShouldReRenderValidationAndAvailableOptions_WhenModelIsInvalid()
    {
        var identityScope = $"openid-{Guid.NewGuid():N}";
        var apiScope = $"api-{Guid.NewGuid():N}";

        await ClientTestDataHelper.SeedIdentityResourcesAsync(_factory, identityScope);
        await ClientTestDataHelper.SeedApiScopesAsync(_factory, apiScope);

        var clientIdentifier = $"interactive-{Guid.NewGuid():N}";
        var clientId = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            clientId: clientIdentifier,
            clientName: "Interactive Client",
            description: "Original description",
            allowedGrantTypes: ["authorization_code"],
            redirectUris: ["https://client.test/callback"],
            postLogoutRedirectUris: ["https://client.test/logout"],
            allowedScopes: [identityScope]);

        var page = await _client.GetAndParsePage($"/admin/clients/{clientId}/edit");
        var form = page.QuerySelector("form").Should().BeAssignableTo<IHtmlFormElement>().Subject;

        var response = await _client.SubmitForm(
            form,
            new Dictionary<string, string>
            {
                ["Input.ClientId"] = string.Empty,
                ["Input.ClientName"] = "Edited Client Name",
                ["Input.Description"] = "Edited description"
            });

        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("The Client ID field is required.");
        document.QuerySelector("input[name='Input.ClientName']")!.GetAttribute("value").Should().Be("Edited Client Name");
        document.QuerySelectorAll("#grant-types-fieldset input[type='checkbox']")
            .Select(x => x.GetAttribute("value"))
            .Should()
            .Contain(["authorization_code", "client_credentials"]);
        document.QuerySelectorAll("#scopes-fieldset input[type='checkbox']")
            .Select(x => x.GetAttribute("value"))
            .Should()
            .Contain([identityScope, apiScope]);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
