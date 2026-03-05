using IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

namespace IdentityServerAspNetIdentity.IntegrationTests.Pages;

public class ErrorIndexIntegrationTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorIndexIntegrationTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Get_ErrorPage_Returns200()
    {
        var response = await _client.GetAsync("/Home/Error");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ErrorPage_ContainsHeading()
    {
        var response = await _client.GetAsync("/Home/Error");
        var document = await AngleSharpHelpers.GetDocumentAsync(response);

        document.Should().NotBeNull();
        var headings = document.QuerySelectorAll("h1, h2");
        headings.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_ErrorPage_ReturnsHtmlContent()
    {
        var response = await _client.GetAsync("/Home/Error");

        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/html");
    }
}
