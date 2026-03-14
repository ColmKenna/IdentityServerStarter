using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages.Admin.Clients;

public class IndexModelIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public IndexModelIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Get_ShouldRenderClientRows_WhenClientsExist()
    {
        // Arrange
        await ClientTestDataHelper.SeedClientAsync(
            _factory,
            "client-one",
            clientName: "Client One",
            description: "First client");

        await ClientTestDataHelper.SeedClientAsync(
            _factory,
            "client-two",
            clientName: "Client Two",
            description: "Second client",
            enabled: false);

        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert
        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        document.Body!.TextContent.Should().Contain("client-one");
        document.Body!.TextContent.Should().Contain("Client One");
        document.Body!.TextContent.Should().Contain("client-two");
        document.Body!.TextContent.Should().Contain("Client Two");
    }

    [Fact]
    public async Task Get_ShouldShowEmptyState_WhenNoClientsExist()
    {
        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert
        document.QuerySelector(".alert-info")!.TextContent
            .Should().Contain("No clients found.");
    }

    [Fact]
    public async Task Get_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        // Arrange
        using var nonAdminFactory = new CustomWebApplicationFactory<NonAdminTestAuthHandler>();
        using var nonAdminClient = nonAdminFactory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        var response = await nonAdminClient.GetAsync("/admin/clients");

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
        var response = await unauthClient.GetAsync("/admin/clients");

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
