using System.Collections;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServerAspNetIdentity;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResource("color", new [] { "favorite_color" }),
            new IdentityResource
            {
                Name = "employees",
                UserClaims = { "canViewEmployees", "canAmendEmployee" },
                DisplayName = "Employee resources",
                Description = "Allow the client to manage employees",
                Emphasize = true
            },
            new IdentityResource
            {
                Name = "products",
                UserClaims = { "canViewProducts", "canAmendProduct" },
                DisplayName = "Product resources",
                Description = "Allow the client to manage products",
                Emphasize = true
            }
        };


    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new ApiScope("api1", "My API")
            {
                UserClaims = new List<string>
                {
                    "canViewEmployees",
                    "canAmendEmployee",
                    "canViewProducts",
                    "canAmendProduct"
                }
            }
        };

    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            // machine to machine client
            new Client
            {
                ClientId = "client",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.ClientCredentials,
                // scopes that client has access to
                AllowedScopes = { "api1" }
            },
                
            // interactive ASP.NET Core Web App
            new Client
            {
                ClientId = "web",
                ClientSecrets = { new Secret("secret".Sha256()) },

                AllowedGrantTypes = GrantTypes.Code,
                    
                // where to redirect to after login
                RedirectUris = { "https://localhost:5002/signin-oidc" },

                // where to redirect to after logout
                PostLogoutRedirectUris = { "https://localhost:5002/signout-callback-oidc" },
    
                AllowOfflineAccess = true,

                AllowedScopes = new List<string>
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "api1",
                    "color",
                    "employees",
                    "products"
                },
                AlwaysIncludeUserClaimsInIdToken = true
            }
        };


}