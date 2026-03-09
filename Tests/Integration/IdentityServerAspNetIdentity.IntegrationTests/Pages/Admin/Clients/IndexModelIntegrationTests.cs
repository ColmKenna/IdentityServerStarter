using Duende.IdentityServer.EntityFramework.DbContexts;
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
    public async Task Get_ShouldShowEmptyState_WhenNoClientsExist()
    {
        var response = await _client.GetAsync("/admin/clients");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector(".alert-info")!.TextContent.Should().Contain("No clients found.");
        document.QuerySelector("ck-responsive-table").Should().BeNull();
    }

    [Fact]
    public async Task Get_ShouldRenderClientRowsAndEditLinks_WhenClientsExist()
    {
        var firstClientId = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            clientId: $"interactive-{Guid.NewGuid():N}",
            clientName: "Interactive Client",
            description: null,
            enabled: true);

        var secondClientId = await ClientTestDataHelper.SeedClientAsync(
            _factory,
            clientId: $"worker-{Guid.NewGuid():N}",
            clientName: "Worker Client",
            description: "Background processing client",
            enabled: false);

        var response = await _client.GetAsync("/admin/clients");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = document.QuerySelectorAll("ck-responsive-tbody ck-responsive-row");
        rows.Should().HaveCount(2);

        var interactiveRow = rows.Single(x => x.TextContent.Contains("Interactive Client"));
        interactiveRow.TextContent.Should().Contain("No description");
        interactiveRow.TextContent.Should().Contain("Enabled");
        interactiveRow.QuerySelector("a")!.GetAttribute("href").Should().EndWith($"/admin/clients/{firstClientId}/edit");

        var workerRow = rows.Single(x => x.TextContent.Contains("Worker Client"));
        workerRow.TextContent.Should().Contain("Background processing client");
        workerRow.TextContent.Should().Contain("Disabled");
        workerRow.QuerySelector("a")!.GetAttribute("href").Should().EndWith($"/admin/clients/{secondClientId}/edit");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
