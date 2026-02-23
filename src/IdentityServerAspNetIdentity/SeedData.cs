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
            
            context.Roles.Add(new IdentityRole { Name = "ADMIN", NormalizedName = "ADMIN" } );
            context.Roles.Add(new IdentityRole { Name = "USER", NormalizedName = "USER" }  );
            context.Roles.Add(new IdentityRole { Name = "GUEST", NormalizedName = "GUEST" });
            context.SaveChanges();
            
            

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

                result = userMgr.AddClaimsAsync(alice, new Claim[]{
                            new Claim(JwtClaimTypes.Name, "Alice Smith"),
                            new Claim(JwtClaimTypes.GivenName, "Alice"),
                            new Claim(JwtClaimTypes.FamilyName, "Smith"),
                            new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
                            new Claim("canViewEmployees", "true"),
                            new Claim("canViewProducts", "true"),
                            new Claim("canAmendEmployee", "true"),
                            new Claim("canAmendProduct", "true")
                        }).Result;
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

                result = userMgr.AddClaimsAsync(bob, new Claim[]{
                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                    new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                    new Claim("location", "somewhere"),
                    new Claim("canViewEmployees", "true"),
                    new Claim("canViewProducts", "true"),
                    // Admin claims — grant all user-admin policies
                    new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUsers),
                    new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserClaims),
                    new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserRoles),
                    new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserGrants),
                    new Claim(UserPolicyConstants.AdminClaimType, UserPolicyConstants.ClaimUserSessions),
                }).Result;
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

            var addToRoleAsync = userMgr.AddToRoleAsync(bob, "ADMIN").Result;
            if (!addToRoleAsync.Succeeded)
            {
                throw new Exception(addToRoleAsync.Errors.First().Description);
            }
            Log.Debug("bob added to admin role");
            
            addToRoleAsync =  userMgr.AddToRoleAsync(bob,"USER").Result;
            if (!addToRoleAsync.Succeeded)
            {
                throw new Exception(addToRoleAsync.Errors.First().Description);
            }
            
            
            addToRoleAsync =  userMgr.AddToRoleAsync(alice,"USER").Result;
            if (!addToRoleAsync.Succeeded)
            {
                throw new Exception(addToRoleAsync.Errors.First().Description);
            }
            
            

        }
    }
}
