using System.Net;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
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

    private void SeedApiScope(string name)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        context.ApiScopes.Add(new ApiScope
        {
            Name = name,
            DisplayName = $"{name} display",
            Description = $"{name} description",
            Enabled = true
        });
        context.SaveChanges();
    }

    [Fact]
    public async Task Get_CreateApiScope_Returns200()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/Create");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_CreateApiScope_RendersFormFields()
    {
        var response = await _client.GetAsync("/Admin/ApiScopes/Create");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.QuerySelector("form[method='post']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.Name']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.DisplayName']").Should().NotBeNull();
        document.QuerySelector("textarea[name='Input.Description']").Should().NotBeNull();
        document.QuerySelector("input[name='Input.Enabled']").Should().NotBeNull();
    }

    [Fact]
    public async Task PostCreate_ValidInput_Returns302RedirectToEdit()
    {
        using var noRedirectClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await noRedirectClient.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Name"] = "orders.read",
                ["Input.DisplayName"] = "Orders Read",
                ["Input.Description"] = "Read orders",
                ["Input.Enabled"] = "true"
            }));

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/Admin/ApiScopes/");
        response.Headers.Location!.ToString().Should().Contain("/Edit");
    }

    [Fact]
    public async Task PostCreate_ValidInput_PersistsApiScope()
    {
        await _client.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Name"] = "orders.write",
                ["Input.DisplayName"] = "Orders Write",
                ["Input.Description"] = "Write orders",
                ["Input.Enabled"] = "false"
            }));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var created = await context.ApiScopes.SingleOrDefaultAsync(s => s.Name == "orders.write");
        created.Should().NotBeNull();
        created!.DisplayName.Should().Be("Orders Write");
        created.Enabled.Should().BeFalse();
    }

    [Fact]
    public async Task PostCreate_DuplicateName_Returns200AndValidationMessage()
    {
        SeedApiScope("orders.read");

        var response = await _client.PostAsync(
            "/Admin/ApiScopes/Create",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Name"] = "orders.read",
                ["Input.DisplayName"] = "Duplicate",
                ["Input.Description"] = "Duplicate",
                ["Input.Enabled"] = "true"
            }));
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.Body!.TextContent.Should().Contain("already exists");
    }
}
