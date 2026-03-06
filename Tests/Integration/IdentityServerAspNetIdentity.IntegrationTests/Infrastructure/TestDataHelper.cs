#nullable enable
using System.Security.Claims;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServerAspNetIdentity.IntegrationTests.Infrastructure;

public static class TestDataHelper
{
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

    public static async Task<string> SeedUserAsync(CustomWebApplicationFactory factory, string usernamePrefix)
    {
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        return await CreateTestUserAsync(
            factory,
            $"{usernamePrefix}-{uniqueSuffix}",
            $"{usernamePrefix}-{uniqueSuffix}@test.local");
    }

    public static async Task AddUserClaimAsync(
        CustomWebApplicationFactory factory,
        string userId,
        string claimType,
        string claimValue)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User '{userId}' not found.");
        var result = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add claim: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }

    public static async Task<string> SeedRoleAsync(CustomWebApplicationFactory factory, string roleName)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        var role = await roleManager.FindByNameAsync(roleName);
        return role!.Id;
    }

    public static async Task AddUserToRoleAsync(
        CustomWebApplicationFactory factory,
        string userId,
        string roleName)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException($"User '{userId}' not found.");
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!createRoleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create role: {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
            }
        }

        var addToRoleResult = await userManager.AddToRoleAsync(user, roleName);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to add user to role: {string.Join(", ", addToRoleResult.Errors.Select(e => e.Description))}");
        }
    }

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

    public static async Task<ApplicationUser?> GetUserByIdAsync(CustomWebApplicationFactory factory, string userId)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId);
    }
}
