using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using FluentAssertions;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ClientsEditIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ClientsEditIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private int SeedClient(
        string clientId = "test-client",
        string clientName = "Test Client",
        string? description = "A test client",
        bool enabled = true,
        List<string>? grantTypes = null,
        List<string>? redirectUris = null,
        List<string>? postLogoutRedirectUris = null,
        List<string>? scopes = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        var client = new Client
        {
            ClientId = clientId,
            ClientName = clientName,
            Description = description,
            Enabled = enabled,
            RequirePkce = true,
            RequireClientSecret = true,
            AccessTokenLifetime = 3600,
            IdentityTokenLifetime = 300,
            SlidingRefreshTokenLifetime = 1296000,
        };

        if (grantTypes != null)
        {
            foreach (var gt in grantTypes)
                client.AllowedGrantTypes.Add(new ClientGrantType { GrantType = gt });
        }

        if (redirectUris != null)
        {
            foreach (var uri in redirectUris)
                client.RedirectUris.Add(new ClientRedirectUri { RedirectUri = uri });
        }

        if (postLogoutRedirectUris != null)
        {
            foreach (var uri in postLogoutRedirectUris)
                client.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = uri });
        }

        if (scopes != null)
        {
            foreach (var s in scopes)
                client.AllowedScopes.Add(new ClientScope { Scope = s });
        }

        context.Clients.Add(client);
        context.SaveChanges();

        return client.Id;
    }

    #region GET Tests

    [Fact]
    public async Task OnGetAsync_WithValidId_ReturnsSuccessAndDisplaysClientData()
    {
        var id = SeedClient(clientName: "My Web App", description: "Main web application");

        var response = await _client.GetAsync($"/admin/clients/{id}/edit");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var document = await AngleSharpHelpers.GetDocumentAsync(response);
        var clientNameInput = document.QuerySelector("input[name='Input.ClientName']");
        clientNameInput.Should().NotBeNull();
        clientNameInput!.GetAttribute("value").Should().Be("My Web App");
    }

    [Fact]
    public async Task OnGetAsync_WithInvalidId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/admin/clients/99999/edit");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OnGetAsync_DisplaysClientIdField()
    {
        var id = SeedClient(clientId: "webapp-client");

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        var clientIdInput = document.QuerySelector("input[name='Input.ClientId']");
        clientIdInput.Should().NotBeNull();
        clientIdInput!.GetAttribute("value").Should().Be("webapp-client");
    }

    [Fact]
    public async Task OnGetAsync_DisplaysEnabledToggle()
    {
        var id = SeedClient(enabled: true);

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        var enabledCheckbox = document.QuerySelector("input[name='Input.Enabled']");
        enabledCheckbox.Should().NotBeNull();
    }

    [Fact]
    public async Task OnGetAsync_DisplaysTokenLifetimeFields()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        document.QuerySelector("input[name='Input.AccessTokenLifetime']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.IdentityTokenLifetime']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.SlidingRefreshTokenLifetime']").Should().NotBeNull();
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task OnPostAsync_WithValidData_UpdatesClientAndRedirects()
    {
        var id = SeedClient(clientName: "Original Name");

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");
        form.Should().NotBeNull();

        var response = await _client.SubmitForm(form!, new Dictionary<string, string>
        {
            { "Input.ClientId", "test-client" },
            { "Input.ClientName", "Updated Name" },
            { "Input.AccessTokenLifetime", "7200" },
            { "Input.IdentityTokenLifetime", "600" },
            { "Input.SlidingRefreshTokenLifetime", "2592000" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the client was updated in the database
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updatedClient = await context.Clients.FindAsync(id);
        updatedClient.Should().NotBeNull();
        updatedClient!.ClientName.Should().Be("Updated Name");
        updatedClient.AccessTokenLifetime.Should().Be(7200);
    }

    [Fact]
    public async Task OnPostAsync_WithInvalidId_ReturnsNotFound()
    {
        // POST directly to a non-existent client
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "Input.ClientId", "fake-client" },
            { "Input.ClientName", "Fake" },
            { "Input.AccessTokenLifetime", "3600" },
            { "Input.IdentityTokenLifetime", "300" },
            { "Input.SlidingRefreshTokenLifetime", "1296000" },
        });

        var response = await _client.PostAsync("/admin/clients/99999/edit", formContent);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OnPostAsync_UpdatesDescription()
    {
        var id = SeedClient(description: "Old description");

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");

        var response = await _client.SubmitForm(form!, new Dictionary<string, string>
        {
            { "Input.ClientId", "test-client" },
            { "Input.ClientName", "Test Client" },
            { "Input.Description", "New description" },
            { "Input.AccessTokenLifetime", "3600" },
            { "Input.IdentityTokenLifetime", "300" },
            { "Input.SlidingRefreshTokenLifetime", "1296000" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updatedClient = await context.Clients.FindAsync(id);
        updatedClient!.Description.Should().Be("New description");
    }

    [Fact]
    public async Task OnPostAsync_CanDisableClient()
    {
        var id = SeedClient(enabled: true);

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");

        // Submit without Enabled checkbox (unchecked = false)
        var response = await _client.SubmitForm(form!, new Dictionary<string, string>
        {
            { "Input.ClientId", "test-client" },
            { "Input.ClientName", "Test Client" },
            { "Input.Enabled", "false" },
            { "Input.AccessTokenLifetime", "3600" },
            { "Input.IdentityTokenLifetime", "300" },
            { "Input.SlidingRefreshTokenLifetime", "1296000" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updatedClient = await context.Clients.FindAsync(id);
        updatedClient!.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task OnPostAsync_CanUpdateBooleanSecuritySettings()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");

        var response = await _client.SubmitForm(form!, new Dictionary<string, string>
        {
            { "Input.ClientId", "test-client" },
            { "Input.ClientName", "Test Client" },
            { "Input.RequirePkce", "true" },
            { "Input.RequireClientSecret", "false" },
            { "Input.RequireConsent", "true" },
            { "Input.AllowOfflineAccess", "true" },
            { "Input.AccessTokenLifetime", "3600" },
            { "Input.IdentityTokenLifetime", "300" },
            { "Input.SlidingRefreshTokenLifetime", "1296000" },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var updatedClient = await context.Clients.FindAsync(id);
        updatedClient!.RequirePkce.Should().BeTrue();
        updatedClient.RequireConsent.Should().BeTrue();
        updatedClient.AllowOfflineAccess.Should().BeTrue();
    }

    [Fact]
    public async Task OnPostAsync_SuccessMessage_IsDisplayedAfterUpdate()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");
        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");

        // The redirect response (after PRG pattern) should contain the success message
        var response = await _client.SubmitForm(form!, new Dictionary<string, string>
        {
            { "Input.ClientId", "test-client" },
            { "Input.ClientName", "Test Client" },
            { "Input.AccessTokenLifetime", "3600" },
            { "Input.IdentityTokenLifetime", "300" },
            { "Input.SlidingRefreshTokenLifetime", "1296000" },
        });

        // After PRG redirect, the success alert should appear
        var resultDoc = await AngleSharpHelpers.GetDocumentAsync(response);
        var successAlert = resultDoc.QuerySelector(".alert-success");
        successAlert.Should().NotBeNull();
        successAlert!.TextContent.Should().Contain("Client updated successfully");
    }

    #endregion

    #region Form Structure Tests

    [Fact]
    public async Task EditPage_HasSubmitButton()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        var submitButton = document.QuerySelector("button[type='submit']");
        submitButton.Should().NotBeNull();
    }

    [Fact]
    public async Task EditPage_HasFormWithPostMethod()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        var form = document.QuerySelector<AngleSharp.Html.Dom.IHtmlFormElement>("form[method='post']");
        form.Should().NotBeNull();
    }

    [Fact]
    public async Task EditPage_DisplaysUriFields()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        document.QuerySelector("input[name='Input.ClientUri']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.LogoUri']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.FrontChannelLogoutUri']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.BackChannelLogoutUri']").Should().NotBeNull();
    }

    [Fact]
    public async Task EditPage_DisplaysSecretFields()
    {
        var id = SeedClient();

        var document = await _client.GetAndParsePage($"/admin/clients/{id}/edit");

        document.QuerySelector("input[name='Input.NewSecret']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.NewSecretDescription']").Should().NotBeNull();
    }

    #endregion
}
