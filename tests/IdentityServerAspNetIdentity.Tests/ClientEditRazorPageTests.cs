using System.Security.Cryptography;
using System.Text;
using AngleSharp;
using AngleSharp.Dom;
using IdentityModel;
using IdentityServer.EF.DataAccess;
using IdentityServerAspNetIdentity.TagHelpers;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace IdentityServerAspNetIdentity.Tests;

using Microsoft.AspNetCore.Razor.TagHelpers;

public class ClientEditRazorPageTests : IClassFixture<WebApplicationFactory<SeedData>>
{
    private WebApplicationFactory<IdentityServerAspNetIdentity.SeedData> _factory;

    public ClientEditRazorPageTests(WebApplicationFactory<IdentityServerAspNetIdentity.SeedData> factory)
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
        mockRepository.Setup(x => x.GetById(TestData.ClientDtModels()[0].ClientId))
            .ReturnsAsync(TestData.ClientDtModels()[0]);

        var mockTabTagHelper = new Mock<TabTagHelper>();

        _factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                ServiceCollectionServiceExtensions.AddSingleton(services, mockRepository.Object);
            });
        });

        var client = await SignIntoClient();

        var response = await client.GetAsync($"/Clients/edit/{TestData.ClientDtModels()[0].ClientId}");
        var content = await response.Content.ReadAsStringAsync();


        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(content));
    }
    
    [Fact]
    public async void ClientEditRazorPageContainsForm()
    {
        var document = await GetParsedDocumentAsync();
        // Should have 1 form
        Assert.Single(document.QuerySelectorAll("form"));
    }
    
    [Fact]
    public async Task TestNavButtonsExist()
    {
        // Act
        var document = await GetParsedDocumentAsync();

        // Assert
        var nav = document.QuerySelector("div.nav.nav-tabs");
        Assert.NotNull(nav);

        // Check for the buttons using more specific selectors
        var redirectUrisButton = nav.QuerySelector("button#RedirectUris_button");
        var postLogoutRedirectUrisButton = nav.QuerySelector("button#PostLogoutRedirectUris_button");
        var allowedScopesButton = nav.QuerySelector("button#AllowedScopes_button");
        var grantTypesButton = nav.QuerySelector("button#GrantTypes_button");
        var otherPropertiesButton = nav.QuerySelector("button#OtherProperties_button");

        Assert.NotNull(redirectUrisButton);
        Assert.Equal("RedirectUris", redirectUrisButton.TextContent.Trim());

        Assert.NotNull(postLogoutRedirectUrisButton);
        Assert.Equal("PostLogoutRedirectUris", postLogoutRedirectUrisButton.TextContent.Trim());

        Assert.NotNull(allowedScopesButton);
        Assert.Equal("AllowedScopes", allowedScopesButton.TextContent.Trim());

        Assert.NotNull(grantTypesButton);
        Assert.Equal("GrantTypes", grantTypesButton.TextContent.Trim());

        Assert.NotNull(otherPropertiesButton);
        Assert.Equal("OtherProperties", otherPropertiesButton.TextContent.Trim());
    }

    [Fact]
    public async void FormContainsSameHtml()
    {
        var document = await GetParsedDocumentAsync();
        var form = document.QuerySelector("form");
        
        // Act
        var inputElement = form.QuerySelector("input[type='text'].d-none");


        
        // Assert
        Assert.NotNull(inputElement);
        Assert.True(inputElement.HasAttribute("readonly"));
        Assert.True(inputElement.HasAttribute("data-val") && inputElement.GetAttribute("data-val") == "true");
        Assert.True(inputElement.HasAttribute("data-val-required") && inputElement.GetAttribute("data-val-required") == "The ClientId field is required.");
        Assert.True(inputElement.HasAttribute("id") && inputElement.GetAttribute("id") == "Client_ClientId");
        Assert.True(inputElement.HasAttribute("name") && inputElement.GetAttribute("name") == "Client.ClientId");
        Assert.True(inputElement.HasAttribute("value") && inputElement.GetAttribute("value") == "web");
        
    }

    [Fact]
    public async void FormContainsTabPanes()
    {
        var document = await GetParsedDocumentAsync();
        var form = document.QuerySelector("form");

        // Select all divs with the tab-pane class inside the form
        IHtmlCollection<IElement> tabPanes = form.QuerySelectorAll("div.tab-pane");
        
        var expectedIds = new List<string>
        {
            "RedirectUris",
            "PostLogoutRedirectUris",
            "AllowedScopes",
            "GrantTypes",
            "OtherProperties"
        };

        foreach (var expectedId in expectedIds)
        {
            var tab = tabPanes.FirstOrDefault(t => t.Id == expectedId);
            Assert.NotNull(tab); // This will fail if a tab with the expected ID is not found
        }
        
        foreach (var expectedId in expectedIds)
        {
            var tab = tabPanes.FirstOrDefault(t => t.Id == expectedId);
            // Check the name attribute
            var nameAttribute = tab.GetAttribute("name");
            Assert.Equal(expectedId , nameAttribute); // This will fail if the name doesn't match the expected name
        }
        
        foreach (var item in expectedIds)
        {
            var tab = tabPanes.FirstOrDefault(t => t.Id == item);
            Assert.NotNull(tab); // This will fail if a tab with the expected ID is not found

            // Assert the class attribute
            var classAttribute = tab.GetAttribute("class");
            Assert.Contains("fade", classAttribute);

            // Assert the name attribute
            var nameAttribute = tab.GetAttribute("name");
            Assert.Equal(item, nameAttribute);

            // Assert the role attribute
            var roleAttribute = tab.GetAttribute("role");
            Assert.Equal("tabpanel", roleAttribute);

            // Assert the aria-labelledby attribute
            var ariaLabelledByAttribute = tab.GetAttribute("aria-labelledby");
            Assert.Equal($"{item}-tab", ariaLabelledByAttribute);
        }
        

    }

    

}