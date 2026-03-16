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

public class ApiScopesAdminServiceTests
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

    private static ApiScopesAdminService CreateSut(ConfigurationDbContext configDb, ApplicationDbContext appDb)
    {
        return new ApiScopesAdminService(configDb, appDb);
    }

    // -------------------------------------------------------------------------
    // GetApiScopesAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetApiScopesAsync_ShouldReturnScopesOrderedByName()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        configDb.ApiScopes.AddRange(
            new ApiScope { Name = "zebra_scope", DisplayName = "Zebra", Enabled = true },
            new ApiScope { Name = "alpha_scope", DisplayName = "Alpha", Enabled = false }
        );
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.GetApiScopesAsync();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("alpha_scope");
        result[1].Name.Should().Be("zebra_scope");
        result[0].Enabled.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // GetForCreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetForCreateAsync_ShouldReturnEmptyInputWithAvailableClaims()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var testUser = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
        appDb.Users.Add(testUser);
        appDb.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" },
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "email", ClaimValue = "test@test.com" }
        );
        await appDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.GetForCreateAsync();

        result.Input.Name.Should().BeEmpty();
        result.Input.Enabled.Should().BeTrue();
        result.AppliedUserClaims.Should().BeEmpty();
        result.AvailableUserClaims.Should().BeEquivalentTo("email", "role");
    }

    // -------------------------------------------------------------------------
    // GetForEditAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNull_WhenScopeDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.GetForEditAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnAvailableClaims_ExcludingAppliedClaims()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope
        {
            Name = "test_scope",
            DisplayName = "Test Scope",
            Enabled = true,
            UserClaims = [
                new ApiScopeClaim { Type = "email" },
                new ApiScopeClaim { Type = "name" }
            ]
        };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var testUser = new ApplicationUser { Id = "u1", UserName = "test_user1", Email = "test1@example.com" };
        appDb.Users.Add(testUser);
        appDb.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "email", ClaimValue = "test@test.com" },
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "name", ClaimValue = "Alice" },
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "role", ClaimValue = "admin" },
            new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "sales" }
        );
        await appDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.GetForEditAsync(scope.Id);

        result.Should().NotBeNull();
        result!.Input.Name.Should().Be("test_scope");
        result.Input.DisplayName.Should().Be("Test Scope");
        result.Input.Enabled.Should().BeTrue();
        result.AppliedUserClaims.Should().BeEquivalentTo("email", "name");
        result.AvailableUserClaims.Should().BeEquivalentTo("department", "role");
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenNameIsUnique()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var request = new CreateApiScopeRequest { Name = " new_scope ", DisplayName = "New Scope", Enabled = true };

        var result = await sut.CreateAsync(request);

        result.Status.Should().Be(CreateApiScopeStatus.Success);
        result.CreatedId.Should().BeGreaterThan(0);

        var savedScope = await configDb.ApiScopes.FindAsync(result.CreatedId);
        savedScope!.Name.Should().Be("new_scope"); // trimmed
        savedScope.ShowInDiscoveryDocument.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDuplicateName_WhenNameAlreadyExists()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        configDb.ApiScopes.Add(new ApiScope { Name = "existing_scope" });
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.CreateAsync(new CreateApiScopeRequest { Name = "existing_scope " });

        result.Status.Should().Be(CreateApiScopeStatus.DuplicateName);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.UpdateAsync(999, new UpdateApiScopeRequest { Name = "test" });

        result.Status.Should().Be(UpdateApiScopeStatus.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnDuplicateName_WhenAnotherScopeHasSameName()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        configDb.ApiScopes.AddRange(
            new ApiScope { Name = "existing_name" },
            new ApiScope { Name = "target_scope" }
        );
        await configDb.SaveChangesAsync();

        var targetScope = await configDb.ApiScopes.SingleAsync(s => s.Name == "target_scope");
        var sut = CreateSut(configDb, appDb);

        var result = await sut.UpdateAsync(targetScope.Id, new UpdateApiScopeRequest { Name = "existing_name " });

        result.Status.Should().Be(UpdateApiScopeStatus.DuplicateName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateFields_WhenValid()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope { Name = "old_name", DisplayName = "Old", Description = "Old desc", Enabled = false };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var request = new UpdateApiScopeRequest
        {
            Name = "new_name",
            DisplayName = "New Display",
            Description = "New desc",
            Enabled = true
        };
        var result = await sut.UpdateAsync(scope.Id, request);

        result.Status.Should().Be(UpdateApiScopeStatus.Success);

        var updated = await configDb.ApiScopes.FindAsync(scope.Id);
        updated!.Name.Should().Be("new_name");
        updated.DisplayName.Should().Be("New Display");
        updated.Description.Should().Be("New desc");
        updated.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldAllowSameName_WhenItBelongsToSameScope()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope { Name = "my_scope", DisplayName = "Original", Enabled = true };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.UpdateAsync(scope.Id, new UpdateApiScopeRequest
        {
            Name = "my_scope",
            DisplayName = "Updated Display",
            Enabled = true
        });

        result.Status.Should().Be(UpdateApiScopeStatus.Success);

        var updated = await configDb.ApiScopes.FindAsync(scope.Id);
        updated!.DisplayName.Should().Be("Updated Display");
    }

    [Fact]
    public async Task CreateAsync_ShouldNullifyWhitespaceOnlyOptionalFields()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var request = new CreateApiScopeRequest
        {
            Name = "test_scope",
            DisplayName = "   ",
            Description = "  ",
            Enabled = true
        };

        var result = await sut.CreateAsync(request);

        result.Status.Should().Be(CreateApiScopeStatus.Success);

        var saved = await configDb.ApiScopes.FindAsync(result.CreatedId);
        saved!.DisplayName.Should().BeNull();
        saved.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldNullifyWhitespaceOnlyOptionalFields()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope { Name = "test_scope", DisplayName = "Has Value", Description = "Has Desc" };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.UpdateAsync(scope.Id, new UpdateApiScopeRequest
        {
            Name = "test_scope",
            DisplayName = "   ",
            Description = "",
            Enabled = true
        });

        result.Status.Should().Be(UpdateApiScopeStatus.Success);

        var updated = await configDb.ApiScopes.FindAsync(scope.Id);
        updated!.DisplayName.Should().BeNull();
        updated.Description.Should().BeNull();
    }

    // -------------------------------------------------------------------------
    // AddClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddClaimAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.AddClaimAsync(999, "role");

        result.Status.Should().Be(AddApiScopeClaimStatus.NotFound);
    }

    [Fact]
    public async Task AddClaimAsync_ShouldReturnAlreadyApplied_WhenClaimExistsOnScope()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope
        {
            Name = "test_scope",
            UserClaims = [new ApiScopeClaim { Type = "role" }]
        };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.AddClaimAsync(scope.Id, "role ");

        result.Status.Should().Be(AddApiScopeClaimStatus.AlreadyApplied);
        result.ClaimType.Should().Be("role");
    }

    [Fact]
    public async Task AddClaimAsync_ShouldAddClaim_WhenNotAlreadyApplied()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope { Name = "test_scope" };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.AddClaimAsync(scope.Id, " email ");

        result.Status.Should().Be(AddApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("email");

        await configDb.Entry(scope).Collection(s => s.UserClaims).LoadAsync();
        scope.UserClaims.Should().ContainSingle(c => c.Type == "email");
    }

    // -------------------------------------------------------------------------
    // RemoveClaimAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();
        var sut = CreateSut(configDb, appDb);

        var result = await sut.RemoveClaimAsync(999, "role");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotFound);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotApplied_WhenClaimDoesNotExistOnScope()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope
        {
            Name = "test_scope",
            UserClaims = [new ApiScopeClaim { Type = "email" }]
        };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.RemoveClaimAsync(scope.Id, "role");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotApplied);
        result.ClaimType.Should().Be("role");
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldRemoveClaim_WhenApplied()
    {
        await using var configDb = CreateConfigurationDbContext();
        await using var appDb = CreateApplicationDbContext();

        var scope = new ApiScope
        {
            Name = "test_scope",
            UserClaims = [
                new ApiScopeClaim { Type = "email" },
                new ApiScopeClaim { Type = "role" }
            ]
        };
        configDb.ApiScopes.Add(scope);
        await configDb.SaveChangesAsync();

        var sut = CreateSut(configDb, appDb);

        var result = await sut.RemoveClaimAsync(scope.Id, " email ");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("email");

        await configDb.Entry(scope).Collection(s => s.UserClaims).LoadAsync();
        scope.UserClaims.Should().ContainSingle(c => c.Type == "role");
    }
}
