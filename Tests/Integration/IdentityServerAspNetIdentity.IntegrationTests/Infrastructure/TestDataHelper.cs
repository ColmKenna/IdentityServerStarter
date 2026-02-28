#nullable enable
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class TestDataHelper
{
    /// <summary>
    /// Creates a test user and returns the user ID.
    /// </summary>
    public static async Task<string> CreateTestUserAsync(
        CustomWebApplicationFactory factory,
        string username,
        string email,
        string? password = null)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password ?? "Pass123$");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user.Id;
    }

    /// <summary>
    /// Deletes a test user by ID.
    /// </summary>
    public static async Task DeleteTestUserAsync(CustomWebApplicationFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await userManager.DeleteAsync(user);
        }
    }

    /// <summary>
    /// Retrieves a user by ID.
    /// </summary>
    public static async Task<ApplicationUser?> GetUserByIdAsync(CustomWebApplicationFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId);
    }
}
