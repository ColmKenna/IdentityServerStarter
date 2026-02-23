using Duende.IdentityServer.EntityFramework.DbContexts;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class TestDatabaseHelper
{
    public static async Task SeedAdminUser(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var existingUser = await userManager.FindByNameAsync("testadmin");
        if (existingUser == null)
        {
            var admin = new ApplicationUser
            {
                UserName = "testadmin",
                Email = "admin@test.com",
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(admin, "Pass123$");
            await userManager.AddToRoleAsync(admin, "ADMIN");
        }
    }

    public static async Task SeedTestUsers(IServiceProvider services, int count = 3)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        for (int i = 1; i <= count; i++)
        {
            var user = new ApplicationUser
            {
                UserName = $"user{i}",
                Email = $"user{i}@test.com",
                EmailConfirmed = true,
            };
            await userManager.CreateAsync(user, "Pass123$");
        }
    }

    public static void SeedTestClients(IServiceProvider services, int count = 3)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();

        for (int i = 1; i <= count; i++)
        {
            context.Clients.Add(new Duende.IdentityServer.EntityFramework.Entities.Client
            {
                ClientId = $"test-client-{i}",
                ClientName = $"Test Client {i}",
                Description = $"Description for client {i}",
                Enabled = i % 2 != 0, // odd = enabled, even = disabled
            });
        }

        context.SaveChanges();
    }
}
