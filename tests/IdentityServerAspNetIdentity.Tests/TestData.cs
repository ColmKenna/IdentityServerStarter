using DataTransferModels;
using IdentityModel;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerAspNetIdentity.Tests;

public static class TestData
{
    public static List<ApplicationUser> Users()
    {
        var users = new List<ApplicationUser>
        {
            new ApplicationUser { Id = "id_johnDoe", UserName = "JohnDoe", Email = "john.doe@example.com" },
            new ApplicationUser { Id = "id_janeSmith", UserName = "JaneSmith", Email = "jane.smith@example.net" },
            new ApplicationUser { Id = "id_bobBrown", UserName = "BobBrown", Email = "bob.brown@example.org" },
            new ApplicationUser { Id = "id_aliceJohnson", UserName = "AliceJohnson", Email = "alice.johnson@example.com" },
            new ApplicationUser { Id = "id_mikeDavis", UserName = "MikeDavis", Email = "mike.davis@example.net" },
            new ApplicationUser { Id = "id_emmaWhite", UserName = "EmmaWhite", Email = "emma.white@example.org" },
            new ApplicationUser { Id = "id_chrisEvans", UserName = "ChrisEvans", Email = "chris.evans@example.com" },
            new ApplicationUser { Id = "id_oliviaTaylor", UserName = "OliviaTaylor", Email = "olivia.taylor@example.net" },
            new ApplicationUser { Id = "id_davidWilson", UserName = "DavidWilson", Email = "david.wilson@example.org" },
            new ApplicationUser { Id = "id_lilyThomas", UserName = "LilyThomas", Email = "lily.thomas@example.com" }
        };

        return users;
    }

    public static List<IdentityUserClaim<string>> IdentityClaims()
    {
        var users = Users();
        var userClaims = new List<IdentityUserClaim<string>>();
        var userClaimValues = new Dictionary<string, List<string>>
        {
            { "JohnDoe", new List<string> { "role:Admin", "permission:Read", "permission:Write", "permission:Delete", "given_name:John", "family_name:Doe", "website:https://johndoe.com" } },
            { "JaneSmith", new List<string> { "role:User", "permission:Read", "given_name:Jane", "family_name:Smith", "website:https://janesmith.net" } },
            { "BobBrown", new List<string> { "role:Manager", "permission:Read", "permission:Write", "given_name:Bob", "family_name:Brown", "website:https://bobbrown.org" } },
            { "AliceJohnson", new List<string> { "role:User", "permission:Read", "given_name:Alice", "family_name:Johnson", "website:https://alicejohnson.com" } },
            { "MikeDavis", new List<string> { "role:Admin", "permission:Read", "permission:Write", "permission:Delete", "given_name:Mike", "family_name:Davis", "website:https://mikedavis.net" } },
            { "EmmaWhite", new List<string> { "role:Guest", "given_name:Emma", "family_name:White", "website:https://emmawhite.org" } },  // Guest might not have any specific permissions
            { "ChrisEvans", new List<string> { "role:Manager", "permission:Read", "permission:Write", "given_name:Chris", "family_name:Evans", "website:https://chrisevans.com" } },
            { "OliviaTaylor", new List<string> { "role:User", "permission:Read", "given_name:Olivia", "family_name:Taylor", "website:https://oliviataylor.net" } },
            { "DavidWilson", new List<string> { "role:User", "permission:Read", "given_name:David", "family_name:Wilson", "website:https://davidwilson.org" } },
            { "LilyThomas", new List<string> { "role:Admin", "permission:Read", "permission:Write", "permission:Delete", "given_name:Lily", "family_name:Thomas", "website:https://lilythomas.com" } }
        };
        
        foreach (var user in users)
        {
            var claimsForUser = userClaimValues[user.UserName];

            // Add name claim for each user
            claimsForUser.Add($"name:{user.UserName}");

            foreach (var claimValue in claimsForUser)
            {
                var parts = claimValue.Split(':');
                var claimType = parts[0];
                var value = parts[1];
            
                userClaims.Add(new IdentityUserClaim<string> { UserId = user.Id, ClaimType = claimType, ClaimValue = value });
            }
        }

        return userClaims;

    }

    public static List<IdentityUserClaim<string>> IdentityClaims(string userId)
    {
        return IdentityClaims().Where(x => x.UserId == userId).ToList();
    }

    public static List<ClientDtModel> ClientDtModels()
    {
        var clients = new List<ClientDtModel>();
        clients.Add(new ClientDtModel()
        {
            ClientId = "web",
            Name = "MVC Client",
            Description = "MVC Client",
            AllowedGrantTypes = new List<string>() {OidcConstants.GrantTypes.AuthorizationCode},
            RedirectUris = new List<string>() {"https://localhost:5002/signin-oidc"},
            PostLogoutRedirectUris = new List<string>() {"https://localhost:5002/signout-callback-oidc"},
            AllowOfflineAccess = true,
            AllowedScopes = new List<string>() {"openid", "profile", "api1", "color"}
        });

        // Adding two more clients
        clients.Add(new ClientDtModel()
        {
            ClientId = "mobile",
            Name = "Mobile Client",
            Description = "Mobile Client",
            AllowedGrantTypes = new List<string>() {OidcConstants.GrantTypes.AuthorizationCode},
            RedirectUris = new List<string>() {"https://localhost:5003/signin-oidc"},
            PostLogoutRedirectUris = new List<string>() {"https://localhost:5003/signout-callback-oidc"},
            AllowOfflineAccess = true,
            AllowedScopes = new List<string>() {"openid", "profile", "api2", "color"}
        });

        clients.Add(new ClientDtModel()
        {
            ClientId = "desktop",
            Name = "Desktop Client",
            Description = "Desktop Client",
            AllowedGrantTypes = new List<string>() {OidcConstants.GrantTypes.AuthorizationCode},
            RedirectUris = new List<string>() {"https://localhost:5004/signin-oidc"},
            PostLogoutRedirectUris = new List<string>() {"https://localhost:5004/signout-callback-oidc"},
            AllowOfflineAccess = true,
            AllowedScopes = new List<string>() {"openid", "profile", "api3", "color"}
        });
        return clients;
    }
}