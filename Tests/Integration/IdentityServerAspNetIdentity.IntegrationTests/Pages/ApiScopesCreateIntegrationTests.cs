using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ApiScopesCreateIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiScopesCreateIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private int SeedApiScope(string name = "orders.read", bool enabled = true)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var apiScope = new ApiScope
        {
            Name = name,
            Enabled = enabled
        };

        context.ApiScopes.Add(apiScope);
        context.SaveChanges();
        return apiScope.Id;
    }

    [Fact]
    public async Task Get_CreateApiScope_Returns200AndRendersEditCreateMode()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/Create");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("h2")!.TextContent.Should().Contain("Create API Scope");
        document.QuerySelector("form#edit-api-scope-form").Should().NotBeNull();
    }

    [Fact]
    public async Task Get_CreateApiScope_DoesNotRedirect()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.GetAsync("/Admin/ApiScopes/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Location.Should().BeNull();
        response.RequestMessage!.RequestUri!.AbsolutePath.Should().Be("/Admin/ApiScopes/Create");
    }

    [Fact]
    public async Task Get_CreateApiScope_RendersUserClaimsTabWithSavePrompt()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/Create");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.QuerySelector("ck-tab[label='User Claims']").Should().NotBeNull();
        var savePrompt = document.QuerySelector("#save-before-user-claims-message");
        savePrompt.Should().NotBeNull();
        savePrompt!.TextContent.Should().Contain("Save API scope");
    }

    [Fact]
    public async Task PostCreate_ValidInput_Returns302RedirectToCreatedScopeEdit()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = "0",
                ["Input.Name"] = "orders.create.redirect",
                ["Input.DisplayName"] = "Orders Create",
                ["Input.Description"] = "Create orders",
                ["Input.Enabled"] = "true"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().MatchRegex("^/Admin/ApiScopes/[1-9][0-9]*/Edit$");
    }

    [Fact]
    public async Task PostCreate_ValidInput_PersistsApiScope()
    {
        await _client.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = "0",
                ["Input.Name"] = "orders.create.persist",
                ["Input.DisplayName"] = "Orders Create Persist",
                ["Input.Description"] = "Create orders persist",
                ["Input.Enabled"] = "false"
            }));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var created = await context.ApiScopes.SingleOrDefaultAsync(apiScope => apiScope.Name == "orders.create.persist");

        created.Should().NotBeNull();
        created!.DisplayName.Should().Be("Orders Create Persist");
        created.Description.Should().Be("Create orders persist");
        created.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task PostCreate_DuplicateName_Returns200AndValidationMessage()
    {
        SeedApiScope(name: "orders.read");

        var response = await _client.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Id"] = "0",
                ["Input.Name"] = "orders.read",
                ["Input.DisplayName"] = "Duplicate Name",
                ["Input.Description"] = "Duplicate",
                ["Input.Enabled"] = "true"
            }));
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("already exists");
    }
}
