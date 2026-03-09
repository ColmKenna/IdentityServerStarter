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

    [Fact]
    public async Task GetApiScopesAsync_ShouldReturnEmptyList_WhenNoScopesExist()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.GetApiScopesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApiScopesAsync_ShouldSortAndMapCorrectly()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        
        configContext.ApiScopes.AddRange(
            new ApiScope { Name = "scope2", DisplayName = null, Description = null, Enabled = true },
            new ApiScope { Name = "scope1", DisplayName = "Scope 1", Description = "Desc 1", Enabled = false }
        );
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.GetApiScopesAsync();

        result.Should().HaveCount(2);
        result.Select(s => s.Name).Should().BeInAscendingOrder();
        
        var scope2 = result.Single(s => s.Name == "scope2");
        scope2.DisplayName.Should().Be(string.Empty);
        scope2.Description.Should().Be(string.Empty);
        scope2.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetForCreateAsync_ShouldReturnAllDistinctClaims()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        
        var testUser = new ApplicationUser { Id = "user1", UserName = "test_user1", Email = "test1@example.com" };
        appContext.Users.Add(testUser);

        appContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "claimB", ClaimValue = "val" },
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "claimA", ClaimValue = "val" },
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "claimB", ClaimValue = "val2" }
        );
        await appContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.GetForCreateAsync();

        result.AppliedUserClaims.Should().BeEmpty();
        result.AvailableUserClaims.Should().HaveCount(2)
            .And.BeInAscendingOrder()
            .And.ContainInOrder("claimA", "claimB");
        result.Input.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNull_WhenScopeNotFound()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.GetForEditAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_ShouldPartitionClaimsCorrectly()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        
        var scope = new ApiScope { Name = "scope1", UserClaims = new List<ApiScopeClaim>() };
        scope.UserClaims.Add(new ApiScopeClaim { Type = "claimA" });
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();

        var testUser = new ApplicationUser { Id = "user1", UserName = "test_user", Email = "test@example.com" };
        appContext.Users.Add(testUser);
        appContext.UserClaims.AddRange(
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "claimA", ClaimValue = "val" },
            new IdentityUserClaim<string> { UserId = "user1", ClaimType = "claimB", ClaimValue = "val" }
        );
        await appContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.GetForEditAsync(scope.Id);

        result!.Input.Name.Should().Be("scope1");
        result.AppliedUserClaims.Should().ContainSingle().Which.Should().Be("claimA");
        result.AvailableUserClaims.Should().ContainSingle().Which.Should().Be("claimB");
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnSuccess_WhenValid()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var sut = new ApiScopesAdminService(configContext, appContext);

        var request = new CreateApiScopeRequest
        {
            Name = "  newScope  ",
            DisplayName = "  New Scope  ",
            Description = " ",
            Enabled = true
        };

        var result = await sut.CreateAsync(request);

        result.Status.Should().Be(CreateApiScopeStatus.Success);
        
        var entity = await configContext.ApiScopes.SingleAsync(s => s.Id == result.CreatedId);
        entity.Name.Should().Be("newScope");
        entity.DisplayName.Should().Be("New Scope");
        entity.Description.Should().BeNull(); // Empty string normalized to null
        entity.ShowInDiscoveryDocument.Should().BeTrue();
        entity.Created.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDuplicateName_WhenNameExists()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        configContext.ApiScopes.Add(new ApiScope { Name = "existingScope" });
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var request = new CreateApiScopeRequest { Name = "existingScope " };
        var result = await sut.CreateAsync(request);

        result.Status.Should().Be(CreateApiScopeStatus.DuplicateName);
        configContext.ApiScopes.Count().Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenScopeMissing()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var sut = new ApiScopesAdminService(configContext, appContext);

        var request = new UpdateApiScopeRequest { Name = "scope" };
        var result = await sut.UpdateAsync(999, request);

        result.Status.Should().Be(UpdateApiScopeStatus.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnDuplicateName_WhenAnotherScopeHasSameName()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        
        var scope1 = new ApiScope { Name = "scope1" };
        var scope2 = new ApiScope { Name = "scope2" };
        configContext.ApiScopes.AddRange(scope1, scope2);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var request = new UpdateApiScopeRequest { Name = "scope2" };
        var result = await sut.UpdateAsync(scope1.Id, request);

        result.Status.Should().Be(UpdateApiScopeStatus.DuplicateName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateScope_WhenValid()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var scope = new ApiScope { Name = "oldName" };
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var request = new UpdateApiScopeRequest
        {
            Name = "newName",
            DisplayName = "display",
            Enabled = false
        };
        
        var result = await sut.UpdateAsync(scope.Id, request);

        result.Status.Should().Be(UpdateApiScopeStatus.Success);
        
        var updated = await configContext.ApiScopes.SingleAsync();
        updated.Name.Should().Be("newName");
        updated.DisplayName.Should().Be("display");
        updated.Enabled.Should().BeFalse();
        updated.Updated.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddClaimAsync_ShouldAddClaim_WhenNotApplied()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var scope = new ApiScope { Name = "scope1" };
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.AddClaimAsync(scope.Id, "  claimA  ");

        result.Status.Should().Be(AddApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("claimA");
        
        var updated = await configContext.ApiScopes.Include(s => s.UserClaims).SingleAsync();
        updated.UserClaims.Should().ContainSingle().Which.Type.Should().Be("claimA");
        updated.Updated.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task AddClaimAsync_ShouldReturnAlreadyApplied_WhenClaimExists()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var scope = new ApiScope { Name = "scope1", UserClaims = new List<ApiScopeClaim>() };
        scope.UserClaims.Add(new ApiScopeClaim { Type = "claimA" });
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.AddClaimAsync(scope.Id, "claimA");

        result.Status.Should().Be(AddApiScopeClaimStatus.AlreadyApplied);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotFound_WhenScopeMissing()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.RemoveClaimAsync(999, "claim");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotFound);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotApplied_WhenClaimMissing()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var scope = new ApiScope { Name = "scope1" };
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.RemoveClaimAsync(scope.Id, "claim");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotApplied);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldRemoveClaim_WhenFound()
    {
        await using var configContext = CreateConfigurationDbContext();
        await using var appContext = CreateApplicationDbContext();
        var scope = new ApiScope { Name = "scope1", UserClaims = new List<ApiScopeClaim>() };
        scope.UserClaims.Add(new ApiScopeClaim { Type = "claimA" });
        configContext.ApiScopes.Add(scope);
        await configContext.SaveChangesAsync();
        
        var sut = new ApiScopesAdminService(configContext, appContext);

        var result = await sut.RemoveClaimAsync(scope.Id, "  claimA  ");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("claimA");
        
        var updated = await configContext.ApiScopes.Include(s => s.UserClaims).SingleAsync();
        updated.UserClaims.Should().BeEmpty();
        updated.Updated.Should().NotBeNull().And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }
}
