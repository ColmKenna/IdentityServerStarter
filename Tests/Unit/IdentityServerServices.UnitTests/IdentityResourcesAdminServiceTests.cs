using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class IdentityResourcesAdminServiceTests
{
    private static ConfigurationDbContext CreateConfigurationDbContext()
    {
        var storeOptions = new ConfigurationStoreOptions();
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var context = new ConfigurationDbContext(options);
        context.StoreOptions = storeOptions;
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static ApplicationDbContext CreateApplicationDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"DataSource=file:{Guid.NewGuid()}?mode=memory&cache=shared")
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
        return context;
    }

    private static IdentityResourcesAdminService CreateSut(ConfigurationDbContext configDb, ApplicationDbContext appDb)
    {
        return new IdentityResourcesAdminService(configDb, appDb);
    }

    // -------------------------------------------------------------------------
    // GetForEditAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetForEditAsync_ShouldReturnAvailableClaims_ExcludingAppliedClaims()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        // Arrange ConfigurationDbContext
        var resource = new IdentityResource
        {
            Name = "profile",
            UserClaims = [
                new IdentityResourceClaim { Type = "email" },
                new IdentityResourceClaim { Type = "name" }
            ]
        };
        configDb.IdentityResources.Add(resource);
        await configDb.SaveChangesAsync();

        // Arrange ApplicationDbContext
        var userId = Guid.NewGuid().ToString();
        appDb.Users.Add(new ApplicationUser { Id = userId, UserName = "testuser", SecurityStamp = Guid.NewGuid().ToString() });
        appDb.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = userId, ClaimType = "email", ClaimValue = "test@test.com" }, // Applied
            new IdentityUserClaim<string> { UserId = userId, ClaimType = "name", ClaimValue = "Alice" },          // Applied
            new IdentityUserClaim<string> { UserId = userId, ClaimType = "role", ClaimValue = "admin" },          // Available
            new IdentityUserClaim<string> { UserId = userId, ClaimType = "department", ClaimValue = "sales" }     // Available
        );
        await appDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        // Act
        var result = await sut.GetForEditAsync(resource.Id);

        // Assert
        result.Should().NotBeNull();
        result!.AppliedUserClaims.Should().BeEquivalentTo("email", "name");
        result.AvailableUserClaims.Should().BeEquivalentTo("department", "role");
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.UpdateAsync(999, new UpdateIdentityResourceRequest { Name = "test" });

        result.Status.Should().Be(UpdateIdentityResourceStatus.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnDuplicateName_WhenAnotherResourceHasSameName()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        configDb.IdentityResources.AddRange(
            new IdentityResource { Name = "existing_name" },
            new IdentityResource { Name = "target_resource" }
        );
        await configDb.SaveChangesAsync();

        var targetResource = await configDb.IdentityResources.SingleAsync(r => r.Name == "target_resource");
        var sut = CreateSut(configDb, appDb);

        // Act: Try to rename 'target_resource' to 'existing_name' (with trailing space to test trim)
        var request = new UpdateIdentityResourceRequest { Name = "existing_name " };
        var result = await sut.UpdateAsync(targetResource.Id, request);

        // Assert
        result.Status.Should().Be(UpdateIdentityResourceStatus.DuplicateName);
    }

    // -------------------------------------------------------------------------
    // AddClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddClaimAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.AddClaimAsync(999, "role");

        result.Status.Should().Be(AddIdentityResourceClaimStatus.NotFound);
    }

    [Fact]
    public async Task AddClaimAsync_ShouldReturnAlreadyApplied_WhenClaimExistsOnResource()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var resource = new IdentityResource
        {
            Name = "profile",
            UserClaims = [new IdentityResourceClaim { Type = "role" }]
        };
        configDb.IdentityResources.Add(resource);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        // Act: Try to add 'role' again (with trailing space to test trim)
        var result = await sut.AddClaimAsync(resource.Id, "role ");

        // Assert
        result.Status.Should().Be(AddIdentityResourceClaimStatus.AlreadyApplied);
        result.ClaimType.Should().Be("role");
    }

    // -------------------------------------------------------------------------
    // RemoveClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.RemoveClaimAsync(999, "role");

        result.Status.Should().Be(RemoveIdentityResourceClaimStatus.NotFound);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotApplied_WhenClaimDoesNotExistOnResource()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var resource = new IdentityResource
        {
            Name = "profile",
            UserClaims = [new IdentityResourceClaim { Type = "email" }] // 'role' is not applied
        };
        configDb.IdentityResources.Add(resource);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        // Act: Try to remove 'role'
        var result = await sut.RemoveClaimAsync(resource.Id, "role");

        // Assert
        result.Status.Should().Be(RemoveIdentityResourceClaimStatus.NotApplied);
        result.ClaimType.Should().Be("role");
    }
}
