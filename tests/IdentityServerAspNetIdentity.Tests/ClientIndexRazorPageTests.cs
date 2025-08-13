using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;
using AngleSharp;
using AngleSharp.Dom;
using Duende.IdentityServer.ResponseHandling;
using IdentityModel;
using IdentityServer.EF.DataAccess;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace IdentityServerAspNetIdentity.Tests;



public class ClientIndexRazorPageTests : IClassFixture<WebApplicationFactory<IdentityServerAspNetIdentity.SeedData>>
{
    private WebApplicationFactory<IdentityServerAspNetIdentity.SeedData> _factory;

    public ClientIndexRazorPageTests(WebApplicationFactory<IdentityServerAspNetIdentity.SeedData> factory)
    {
        _factory = factory;
    }


    private async Task<HttpClient> SignIntoClient()
    {
        var client = _factory.CreateClient();

        string codeVerifier = RandomString(32); // generate a new code verifier
        string codeChallenge = Base64Url.Encode(Sha256(codeVerifier)); // generate the code challenge

        var authorizeResponse = await client.GetAsync("/connect/authorize?" +
                                                      "client_id=web&" +
                                                      "response_type=code&" +
                                                      "scope=openid profile api1 color&" +
                                                      "redirect_uri=https://localhost:5002/signin-oidc&" +
                                                      "nonce=random_nonce&" +
                                                      "state=random_state&" +
                                                      "code_challenge=" + codeChallenge + "&" +
                                                      "code_challenge_method=S256");


        var context = BrowsingContext.New(Configuration.Default);
        var authorizeContent = await authorizeResponse.Content.ReadAsStringAsync();
        var document = await context.OpenAsync(req => req.Content(authorizeContent));
        var antiForgeryToken = document.QuerySelector("input[name='__RequestVerificationToken']").GetAttribute("value");


        // 2. Submit the login form (assuming the default login page from IdentityServer quickstart UI)
        var loginContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Username", "alice"),
            new KeyValuePair<string, string>("Password", "Pass123$"),
            new KeyValuePair<string, string>("button", "login"),
            new KeyValuePair<string, string>("__RequestVerificationToken", antiForgeryToken) // <--- Include this line
        });

        var loginResponse = await client.PostAsync("/Account/Login", loginContent);
        return client;
    }

    private static string RandomString(int length)
    {
        RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        byte[] randomBytes = new byte[length];
        provider.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static byte[] Sha256(string randomString)
    {
        SHA256 sha256 = SHA256.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(randomString);
        byte[] hash = sha256.ComputeHash(inputBytes);
        return hash;
    }
    
    private async Task<IDocument> GetParsedDocumentAsync()
    {
        var mockRepository = new Mock<IClientsRepository>();
        mockRepository.Setup(x => x.GetAllClients()).ReturnsAsync(TestData.ClientDtModels());

        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => { services.AddSingleton(mockRepository.Object); });
        });

        var client = await SignIntoClient();

        // Fetching the content from the client list page
        var response = await client.GetAsync("/Clients/Index");
        var content = await response.Content.ReadAsStringAsync();

        // Parse the content using AngleSharp
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(content));
    }
    
    
        [Fact]
    public async Task TableExistsWithCorrectClass()
    {
        var document = await GetParsedDocumentAsync();
        var table = document.QuerySelector("table");
        Assert.NotNull(table);
        Assert.Equal("table table-bordered table-striped", table.GetAttribute("class"));
    }

    [Fact]
    public async Task TheadExistsWithCorrectClass()
    {
        var document = await GetParsedDocumentAsync();
        var thead = document.QuerySelector("table > thead");
        Assert.NotNull(thead);
        Assert.Equal("thead-dark", thead.GetAttribute("class"));
    }

    [Fact]
    public async Task TheadHasSingleTr()
    {
        var document = await GetParsedDocumentAsync();
        var trs = document.QuerySelectorAll("table > thead > tr");
        Assert.Single(trs);
    }

    [Fact]
    public async Task TheadHasCorrectThValues()
    {
        var document = await GetParsedDocumentAsync();
        var ths = document.QuerySelectorAll("table > thead > tr > th");
        var expectedHeaders = new List<string>
        {
            "Client Id", "Allowed Grant Types", "Redirect Uris", 
            "Post Logout Redirect Uris", "Allow Offline Access", 
            "Allowed Scopes", ""
        };
        Assert.Equal(expectedHeaders.Count, ths.Length);
        for (int i = 0; i < expectedHeaders.Count; i++)
        {
            Assert.Equal(expectedHeaders[i], ths[i].TextContent.Trim());
        }
    }

    [Fact]
    public async Task TbodyExists()
    {
        var document = await GetParsedDocumentAsync();
        var tbody = document.QuerySelector("table > tbody");
        Assert.NotNull(tbody);
    }

    [Fact]
    public async Task TbodyHasThreeRows()
    {
        var document = await GetParsedDocumentAsync();
        var trs = document.QuerySelectorAll("table > tbody > tr");
        Assert.Equal(3, trs.Length);
    }

    [Fact]
    public async Task RowsContainExpectedData()
    {
        var document = await GetParsedDocumentAsync();

        // Extract table rows (excluding header row)
        var tableRows = document.QuerySelectorAll("table.table tbody tr");

        // Assert we have the expected number of rows
        Assert.Equal(3, tableRows.Length);  // 3 rows are expected based on provided data

        var testClients = TestData.ClientDtModels();

        for (int i = 0; i < tableRows.Length; i++)
        {
            var cells = tableRows[i].QuerySelectorAll("td").ToArray();
            Assert.Equal(testClients[i].ClientId, cells[0].TextContent.Trim());
            Assert.Contains(testClients[i].AllowedGrantTypes[0], cells[1].TextContent);
            Assert.Contains(testClients[i].RedirectUris[0], cells[2].TextContent);
            Assert.Contains(testClients[i].PostLogoutRedirectUris[0], cells[3].TextContent);
            Assert.Equal(testClients[i].AllowOfflineAccess ? "Yes" : "No", cells[4].TextContent.Trim());
            foreach (var scope in testClients[i].AllowedScopes)
            {
                Assert.Contains(scope, cells[5].TextContent);
            }
            var editLink = cells[6].QuerySelector("a");
            Assert.NotNull(editLink);
            Assert.Equal($"/Clients/Edit/{testClients[i].ClientId}", editLink.GetAttribute("href"));

        }
    }


}