using System.Security.Claims;
using IdentityModel;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IdentityServerAspNetIdentity;

public class SeedData
{
    public static void EnsureSeedData(WebApplication app)
    {
        using (var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope())
        {
            var context = scope.ServiceProvider.GetService<ApplicationDbContext>();
            context.Database.EnsureDeleted();
            context.Database.Migrate();

            context.Roles.Add(new IdentityRole { Name = "ADMIN", NormalizedName = "ADMIN" });
            context.Roles.Add(new IdentityRole { Name = "USER", NormalizedName = "USER" });
            context.Roles.Add(new IdentityRole { Name = "GUEST", NormalizedName = "GUEST" });
            context.SaveChanges();

            var aliceRequiredClaims = new Claim[]
            {
                new Claim(JwtClaimTypes.Name, "Alice Smith"),
                new Claim(JwtClaimTypes.GivenName, "Alice"),
                new Claim(JwtClaimTypes.FamilyName, "Smith"),
                new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                new Claim("canViewEmployees", "true"),
                new Claim("canViewProducts", "true"),
                new Claim("canAmendEmployee", "true"),
                new Claim("canAmendProduct", "true"),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserGrants),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserSessions),
            };

            var bobRequiredClaims = new Claim[]
            {
                new Claim(JwtClaimTypes.Name, "Bob Smith"),
                new Claim(JwtClaimTypes.GivenName, "Bob"),
                new Claim(JwtClaimTypes.FamilyName, "Smith"),
                new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                new Claim("location", "somewhere"),
                new Claim("canViewEmployees", "true"),
                new Claim("canViewProducts", "true"),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserGrants),
                new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserSessions),
            };

            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var alice = userMgr.FindByNameAsync("alice").Result;

            if (alice == null)
            {
                alice = new ApplicationUser
                {
                    UserName = "alice",
                    Email = "AliceSmith@email.com",
                    EmailConfirmed = true,
                    FavoriteColor = "red",
                };
                var result = userMgr.CreateAsync(alice, "Pass123$").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                Log.Debug("alice created");
            }
            else
            {
                Log.Debug("alice already exists");
            }

            EnsureUserClaims(userMgr, alice, aliceRequiredClaims);

            var bob = userMgr.FindByNameAsync("bob").Result;
            if (bob == null)
            {
                bob = new ApplicationUser
                {
                    UserName = "bob",
                    Email = "BobSmith@email.com",
                    EmailConfirmed = true
                };
                var result = userMgr.CreateAsync(bob, "Pass123$").Result;
                if (!result.Succeeded)
                {
                    throw new Exception(result.Errors.First().Description);
                }

                Log.Debug("bob created");
            }
            else
            {
                Log.Debug("bob already exists");
            }

            EnsureUserClaims(userMgr, bob, bobRequiredClaims);

            EnsureUserInRole(userMgr, bob, "ADMIN");
            EnsureUserInRole(userMgr, bob, "USER");
            EnsureUserInRole(userMgr, alice, "USER");
            EnsureUserInRole(userMgr, alice, "ADMIN");
        }
    }

    private static void EnsureUserInRole(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName)
    {
        if (userManager.IsInRoleAsync(user, roleName).Result)
        {
            Log.Debug("{UserName} already in role {RoleName}", user.UserName, roleName);
            return;
        }

        var result = userManager.AddToRoleAsync(user, roleName).Result;
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        Log.Debug("{UserName} added to role {RoleName}", user.UserName, roleName);
    }

    private static void EnsureUserClaims(UserManager<ApplicationUser> userManager, ApplicationUser user, IEnumerable<Claim> requiredClaims)
    {
        var existingClaims = userManager.GetClaimsAsync(user).Result;
        var claimsToAdd = requiredClaims
            .Where(required => !existingClaims.Any(existing => existing.Type == required.Type && existing.Value == required.Value))
            .ToList();

        if (!claimsToAdd.Any())
        {
            Log.Debug("{UserName} already has all required claims", user.UserName);
            return;
        }

        var result = userManager.AddClaimsAsync(user, claimsToAdd).Result;
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        Log.Debug("{UserName} missing claims added: {Count}", user.UserName, claimsToAdd.Count);
    }
}
