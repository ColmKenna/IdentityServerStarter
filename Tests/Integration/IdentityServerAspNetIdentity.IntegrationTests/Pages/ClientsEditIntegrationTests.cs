using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ClientsEditIntegrationTests : IClassFixture<ClientsEditIntegrationTests.EditPageFactory>
{
    private readonly HttpClient _client;
    private readonly EditPageFactory _factory;

    public ClientsEditIntegrationTests(EditPageFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // Phase 1 — GET returns success
    [Fact]
    public async Task should_return_success_when_get_edit_with_valid_id()
    {
        // Arrange
        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(CreateValidViewModel());

        // Act
        var response = await _client.GetAsync("/admin/clients/1/edit");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    // Phase 1 — GET returns not found for unknown client
    [Fact]
    public async Task should_return_not_found_when_get_edit_with_invalid_id()
    {
        // Arrange
        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(9999))
            .ReturnsAsync((ClientEditViewModel?)null);

        // Act
        var response = await _client.GetAsync("/admin/clients/9999/edit");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // Phase 2 — Edit page displays client data
    [Fact]
    public async Task should_display_client_data_when_get_edit()
    {
        // Arrange
        var vm = CreateValidViewModel();
        vm.ClientId = "my-special-client";
        vm.ClientName = "My Special Client";

        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(2))
            .ReturnsAsync(vm);

        // Act
        var document = await _client.GetAndParsePage("/admin/clients/2/edit");

        // Assert
        var clientIdInput = document.QuerySelector("input[name='Input.ClientId']") as IHtmlInputElement;
        clientIdInput.Should().NotBeNull();
        clientIdInput!.Value.Should().Be("my-special-client");

        var clientNameInput = document.QuerySelector("input[name='Input.ClientName']") as IHtmlInputElement;
        clientNameInput.Should().NotBeNull();
        clientNameInput!.Value.Should().Be("My Special Client");
    }

    // Phase 5 — DOM: page title
    [Fact]
    public async Task should_contain_edit_heading_when_get_edit()
    {
        // Arrange
        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(CreateValidViewModel());

        // Act
        var document = await _client.GetAndParsePage("/admin/clients/1/edit");

        // Assert
        var heading = document.QuerySelector("h2");
        heading.Should().NotBeNull();
        heading!.TextContent.Should().Contain("Edit Client");
    }

    // Phase 5 — DOM: form has submit button
    [Fact]
    public async Task should_have_submit_button_when_get_edit()
    {
        // Arrange
        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(CreateValidViewModel());

        // Act
        var document = await _client.GetAndParsePage("/admin/clients/1/edit");

        // Assert
        var submitButton = document.QuerySelector("button[type='submit']");
        submitButton.Should().NotBeNull();
        submitButton!.TextContent.Should().Contain("Save Changes");
    }

    // Phase 5 — DOM: form has back link
    [Fact]
    public async Task should_have_back_link_when_get_edit()
    {
        // Arrange
        _factory.MockClientEditor
            .Setup(s => s.GetClientForEditAsync(1))
            .ReturnsAsync(CreateValidViewModel());

        // Act
        var document = await _client.GetAndParsePage("/admin/clients/1/edit");

        // Assert
        var backLink = document.QuerySelector("a.btn-secondary");
        backLink.Should().NotBeNull();
        backLink!.TextContent.Should().Contain("Back to Home");
    }

    private static ClientEditViewModel CreateValidViewModel()
    {
        return new ClientEditViewModel
        {
            ClientId = "test-client",
            ClientName = "Test Client",
            Description = "A test client",
            Enabled = true,
            RequirePkce = true,
            RequireClientSecret = true,
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300,
            SlidingRefreshTokenLifetime = 1296000,
            AllowedGrantTypes = new List<string> { "authorization_code" },
            AllowedScopes = new List<string> { "openid", "profile" },
            RedirectUris = new List<string> { "https://localhost/callback" },
            PostLogoutRedirectUris = new List<string> { "https://localhost/signout" },
            AvailableScopes = new List<string> { "openid", "profile", "api1" },
            AvailableGrantTypes = new List<string> { "authorization_code", "client_credentials" },
        };
    }

    /// <summary>
    /// A customised factory for Edit page tests that replaces IClientEditor with a mock.
    /// </summary>
    public class EditPageFactory : CustomWebApplicationFactory
    {
        public Mock<IClientEditor> MockClientEditor { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                // Remove the real IClientEditor and register the mock
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IClientEditor));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddSingleton(MockClientEditor.Object);
            });
        }
    }
}
