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
    public static async Task EnsureSeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        context.Roles.Add(new IdentityRole { Name = "ADMIN", NormalizedName = "ADMIN" });
        context.Roles.Add(new IdentityRole { Name = "USER", NormalizedName = "USER" });
        context.Roles.Add(new IdentityRole { Name = "GUEST", NormalizedName = "GUEST" });
        await context.SaveChangesAsync();

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
        var alice = await userMgr.FindByNameAsync("alice");

        if (alice == null)
        {
            alice = new ApplicationUser
            {
                UserName = "alice",
                Email = "AliceSmith@email.com",
                EmailConfirmed = true,
                FavoriteColor = "red",
            };
            var result = await userMgr.CreateAsync(alice, "Pass123$");
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

        await EnsureUserClaimsAsync(userMgr, alice, aliceRequiredClaims);

        var bob = await userMgr.FindByNameAsync("bob");
        if (bob == null)
        {
            bob = new ApplicationUser
            {
                UserName = "bob",
                Email = "BobSmith@email.com",
                EmailConfirmed = true
            };
            var result = await userMgr.CreateAsync(bob, "Pass123$");
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

        await EnsureUserClaimsAsync(userMgr, bob, bobRequiredClaims);

        await EnsureUserInRoleAsync(userMgr, bob, "ADMIN");
        await EnsureUserInRoleAsync(userMgr, bob, "USER");
        await EnsureUserInRoleAsync(userMgr, alice, "USER");
        await EnsureUserInRoleAsync(userMgr, alice, "ADMIN");
    }

    private static async Task EnsureUserInRoleAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, string roleName)
    {
        if (await userManager.IsInRoleAsync(user, roleName))
        {
            Log.Debug("{UserName} already in role {RoleName}", user.UserName, roleName);
            return;
        }

        var result = await userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        Log.Debug("{UserName} added to role {RoleName}", user.UserName, roleName);
    }

    private static async Task EnsureUserClaimsAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, IEnumerable<Claim> requiredClaims)
    {
        var existingClaims = await userManager.GetClaimsAsync(user);
        var claimsToAdd = requiredClaims
            .Where(required => !existingClaims.Any(existing => existing.Type == required.Type && existing.Value == required.Value))
            .ToList();

        if (!claimsToAdd.Any())
        {
            Log.Debug("{UserName} already has all required claims", user.UserName);
            return;
        }

        var result = await userManager.AddClaimsAsync(user, claimsToAdd);
        if (!result.Succeeded)
        {
            throw new Exception(result.Errors.First().Description);
        }

        Log.Debug("{UserName} missing claims added: {Count}", user.UserName, claimsToAdd.Count);
    }
}
