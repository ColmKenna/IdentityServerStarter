using Duende.IdentityServer.EntityFramework.DbContexts;
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
    public async Task Get_ShouldRenderEditFormWithClientData_WhenClientExists()
    {
        // Arrange
        await ClientTestDataHelper.SeedIdentityResourcesAsync(_factory, "openid");
        await ClientTestDataHelper.SeedApiScopesAsync(_factory, "api1");
        var id = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            "test-client",
            clientName: "Test Client",
            description: "A test client");

        // Act
        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        // Assert
        document.QuerySelector("h2")!.TextContent.Should().Contain("Edit Client");

        var clientIdInput = document.QuerySelector<IHtmlInputElement>("#Input_ClientId");
        clientIdInput!.Value.Should().Be("test-client");

        var clientNameInput = document.QuerySelector<IHtmlInputElement>("#Input_ClientName");
        clientNameInput!.Value.Should().Be("Test Client");

        var descriptionTextarea = document.QuerySelector<IHtmlTextAreaElement>("#Input_Description");
        descriptionTextarea!.Value.Should().Be("A test client");
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenClientDoesNotExist()
    {
        // Act
        var response = await _client.GetAsync("/admin/clients/99999/edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ShouldUpdateClientAndRedirect_WhenDataIsValid()
    {
        // Arrange
        await ClientTestDataHelper.SeedIdentityResourcesAsync(_factory, "openid");
        await ClientTestDataHelper.SeedApiScopesAsync(_factory, "api1");
        var id = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            "client-to-update",
            clientName: "Original Client",
            description: "Original Description",
            allowedGrantTypes: ["authorization_code"],
            allowedScopes: ["openid"]);

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<IHtmlFormElement>("form")!;

        // Act
        var response = await _client.SubmitForm(form, new Dictionary<string, string>
        {
            ["Input.ClientId"] = "updated-client",
            ["Input.ClientName"] = "Updated Client",
            ["Input.Description"] = "Updated Description"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultDocument = await AngleSharpHelpers.GetDocumentAsync(response);

        resultDocument.QuerySelector(".alert-success")!.TextContent
            .Should().Contain("Client updated successfully");

        var clientIdInput = resultDocument.QuerySelector<IHtmlInputElement>("#Input_ClientId");
        clientIdInput!.Value.Should().Be("updated-client");

        var clientNameInput = resultDocument.QuerySelector<IHtmlInputElement>("#Input_ClientName");
        clientNameInput!.Value.Should().Be("Updated Client");

        using var scope = _factory.Services.CreateScope();
        var configDbContext = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        var updatedClient = await configDbContext.Clients
            .FirstOrDefaultAsync(c => c.Id == id);

        updatedClient.Should().NotBeNull();
        updatedClient!.ClientId.Should().Be("updated-client");
        updatedClient.ClientName.Should().Be("Updated Client");
        updatedClient.Description.Should().Be("Updated Description");
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await nonAdminClient.GetAsync("/admin/clients/0/edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        using var unauthFactory = new CustomWebApplicationFactory<UnauthenticatedTestAuthHandler>();
        using var unauthClient = unauthFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await unauthClient.GetAsync("/admin/clients/0/edit");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
