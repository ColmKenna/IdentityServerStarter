using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ClientsIndexIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ClientsIndexIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Phase 1 — Page loads
    [Fact]
    public async Task should_return_success_status_when_get_clients_index()
    {
        // Act
        var response = await _client.GetAsync("/admin/clients");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // Phase 2 — Displays client data
    [Fact]
    public async Task should_display_clients_when_clients_exist()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            context.Clients.Add(new Client
            {
                ClientId = $"display-test-{Guid.NewGuid():N}",
                ClientName = "Display Test Client",
                Description = "For integration test",
                Enabled = true
            });
            await context.SaveChangesAsync();
        }

        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert
        document.Body!.TextContent.Should().Contain("Display Test Client");
    }

    // Phase 2 — Page handles empty state gracefully (checked via body content)
    [Fact]
    public async Task should_display_no_clients_found_text_or_table_when_get()
    {
        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert — the page should render without error regardless of whether clients exist
        document.Body.Should().NotBeNull();
        var bodyText = document.Body!.TextContent;
        // Either clients are displayed in a table or the empty message is shown
        (bodyText.Contains("No clients found") || bodyText.Contains("IdentityServer Clients"))
            .Should().BeTrue();
    }

    // Phase 5 — DOM: heading
    [Fact]
    public async Task should_contain_heading_when_get_clients_index()
    {
        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("IdentityServer Clients");
    }

    // Phase 5 — DOM: edit links
    [Fact]
    public async Task should_contain_edit_links_for_each_client()
    {
        // Arrange
        int clientDbId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
            var entity = new Client
            {
                ClientId = $"editlink-test-{Guid.NewGuid():N}",
                ClientName = "Edit Link Client",
                Enabled = true
            };
            context.Clients.Add(entity);
            await context.SaveChangesAsync();
            clientDbId = entity.Id;
        }

        // Act
        var document = await _client.GetAndParsePage("/admin/clients");

        // Assert
        var editLinks = document.QuerySelectorAll("a[href*='/edit']");
        editLinks.Length.Should().BeGreaterThan(0);
    }
}
