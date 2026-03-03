using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class ApiScopesAdminServiceEditTests : IDisposable
{
    private readonly SqliteConnection _configurationConnection;
    private readonly SqliteConnection _applicationConnection;
    private readonly DbContextOptions<ConfigurationDbContext> _configurationOptions;
    private readonly DbContextOptions<ApplicationDbContext> _applicationOptions;

    public ApiScopesAdminServiceEditTests()
    {
        _configurationConnection = new SqliteConnection("Data Source=:memory:");
        _configurationConnection.Open();

        _applicationConnection = new SqliteConnection("Data Source=:memory:");
        _applicationConnection.Open();

        var storeOptions = new ConfigurationStoreOptions();
        _configurationOptions = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlite(_configurationConnection)
            .UseApplicationServiceProvider(
                new ServiceCollection()
                    .AddSingleton(storeOptions)
                    .BuildServiceProvider())
            .Options;

        _applicationOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_applicationConnection)
            .Options;

        using var configurationDbContext = CreateConfigurationContext();
        configurationDbContext.Database.EnsureCreated();

        using var applicationDbContext = CreateApplicationContext();
        applicationDbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _configurationConnection.Dispose();
        _applicationConnection.Dispose();
    }

    private ConfigurationDbContext CreateConfigurationContext()
    {
        return new ConfigurationDbContext(_configurationOptions);
    }

    private ApplicationDbContext CreateApplicationContext()
    {
        return new ApplicationDbContext(_applicationOptions);
    }

    private ApiScopesAdminService CreateService(
        ConfigurationDbContext configurationDbContext,
        ApplicationDbContext applicationDbContext)
    {
        return new ApiScopesAdminService(configurationDbContext, applicationDbContext);
    }

    [Fact]
    public async Task GetForCreateAsync_ReturnsDefaultsAndKnownClaimsOrderedDistinct()
    {
        using (var seedApplicationDbContext = CreateApplicationContext())
        {
            seedApplicationDbContext.Users.AddRange(
                new ApplicationUser { Id = "u1", UserName = "u1", NormalizedUserName = "U1" },
                new ApplicationUser { Id = "u2", UserName = "u2", NormalizedUserName = "U2" },
                new ApplicationUser { Id = "u3", UserName = "u3", NormalizedUserName = "U3" });
            seedApplicationDbContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "region", ClaimValue = "eu" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "department", ClaimValue = "engineering" },
                new IdentityUserClaim<string> { UserId = "u3", ClaimType = "region", ClaimValue = "na" });
            await seedApplicationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetForCreateAsync();

        result.Input.Name.Should().BeEmpty();
        result.Input.Enabled.Should().BeTrue();
        result.AppliedUserClaims.Should().BeEmpty();
        result.AvailableUserClaims.Should().Equal("department", "region");
    }

    [Fact]
    public async Task GetForEditAsync_Missing_ReturnsNull()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetForEditAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetForEditAsync_ReturnsInputAndClaimLists_WithTrimDistinctOrderAndAvailableMinusApplied()
    {
        int id;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                DisplayName = "Orders Read",
                Description = "Read orders",
                Enabled = true,
                UserClaims = new List<ApiScopeClaim>
                {
                    new() { Type = " department " },
                    new() { Type = "location" },
                    new() { Type = "department" },
                    new() { Type = "  " }
                }
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
        }

        using (var seedApplicationDbContext = CreateApplicationContext())
        {
            seedApplicationDbContext.Users.AddRange(
                new ApplicationUser { Id = "u1", UserName = "u1", NormalizedUserName = "U1" },
                new ApplicationUser { Id = "u2", UserName = "u2", NormalizedUserName = "U2" },
                new ApplicationUser { Id = "u3", UserName = "u3", NormalizedUserName = "U3" },
                new ApplicationUser { Id = "u4", UserName = "u4", NormalizedUserName = "U4" });
            seedApplicationDbContext.UserClaims.AddRange(
                new IdentityUserClaim<string> { UserId = "u1", ClaimType = "department", ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u2", ClaimType = "location", ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u3", ClaimType = "region", ClaimValue = "x" },
                new IdentityUserClaim<string> { UserId = "u4", ClaimType = "region", ClaimValue = "x" });
            await seedApplicationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.GetForEditAsync(id);

        result.Should().NotBeNull();
        result!.Input.Name.Should().Be("orders.read");
        result.Input.DisplayName.Should().Be("Orders Read");
        result.Input.Description.Should().Be("Read orders");
        result.Input.Enabled.Should().BeTrue();
        result.AppliedUserClaims.Should().Equal("department", "location");
        result.AvailableUserClaims.Should().Equal("region");
    }

    [Fact]
    public async Task CreateAsync_DuplicateName_ReturnsDuplicateName()
    {
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            seedConfigurationDbContext.ApiScopes.Add(new ApiScope { Name = "orders.read", Enabled = true });
            await seedConfigurationDbContext.SaveChangesAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.CreateAsync(new CreateApiScopeRequest
        {
            Name = "orders.read",
            Enabled = true
        });

        result.Status.Should().Be(CreateApiScopeStatus.DuplicateName);
        result.CreatedId.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_Success_PersistsWithTrimmedName_NormalizedOptionals_DefaultFlags_Timestamps()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);
        var before = DateTime.UtcNow;

        var result = await service.CreateAsync(new CreateApiScopeRequest
        {
            Name = "  orders.create  ",
            DisplayName = "  Orders Create  ",
            Description = "   ",
            Enabled = false
        });
        var after = DateTime.UtcNow;

        result.Status.Should().Be(CreateApiScopeStatus.Success);
        result.CreatedId.Should().BeGreaterThan(0);

        var created = await configurationDbContext.ApiScopes.FindAsync(result.CreatedId);
        created.Should().NotBeNull();
        created!.Name.Should().Be("orders.create");
        created.DisplayName.Should().Be("Orders Create");
        created.Description.Should().BeNull();
        created.Enabled.Should().BeFalse();
        created.ShowInDiscoveryDocument.Should().BeTrue();
        created.Required.Should().BeFalse();
        created.Emphasize.Should().BeFalse();
        created.NonEditable.Should().BeFalse();
        created.Created.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        created.Updated.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public async Task UpdateAsync_Missing_ReturnsNotFound()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.UpdateAsync(999, new UpdateApiScopeRequest
        {
            Name = "orders.updated",
            Enabled = true
        });

        result.Status.Should().Be(UpdateApiScopeStatus.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateName_ReturnsDuplicateName()
    {
        int id;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            seedConfigurationDbContext.ApiScopes.AddRange(
                new ApiScope { Name = "orders.read", Enabled = true },
                new ApiScope { Name = "orders.write", Enabled = true });
            await seedConfigurationDbContext.SaveChangesAsync();
            id = await seedConfigurationDbContext.ApiScopes
                .Where(scope => scope.Name == "orders.write")
                .Select(scope => scope.Id)
                .SingleAsync();
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.UpdateAsync(id, new UpdateApiScopeRequest
        {
            Name = "orders.read",
            Enabled = true
        });

        result.Status.Should().Be(UpdateApiScopeStatus.DuplicateName);
    }

    [Fact]
    public async Task UpdateAsync_Success_UpdatesTrimmedAndNormalizedFields_UpdatesTimestamp()
    {
        int id;
        DateTime? originalUpdated;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                DisplayName = "Orders Read",
                Description = "Read orders",
                Enabled = true,
                Updated = DateTime.UtcNow.AddDays(-1)
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
            originalUpdated = apiScope.Updated;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.UpdateAsync(id, new UpdateApiScopeRequest
        {
            Name = "  orders.write  ",
            DisplayName = "  Orders Write  ",
            Description = " ",
            Enabled = false
        });

        result.Status.Should().Be(UpdateApiScopeStatus.Success);

        var updated = await configurationDbContext.ApiScopes.FindAsync(id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("orders.write");
        updated.DisplayName.Should().Be("Orders Write");
        updated.Description.Should().BeNull();
        updated.Enabled.Should().BeFalse();
        updated.Updated.Should().HaveValue();
        updated.Updated!.Value.Should().BeAfter(originalUpdated!.Value);
    }

    [Fact]
    public async Task AddClaimAsync_MissingScope_ReturnsNotFound()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.AddClaimAsync(999, "department");

        result.Status.Should().Be(AddApiScopeClaimStatus.NotFound);
    }

    [Fact]
    public async Task AddClaimAsync_AlreadyApplied_ReturnsAlreadyApplied()
    {
        int id;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                Enabled = true,
                UserClaims = new List<ApiScopeClaim>
                {
                    new() { Type = "department" }
                }
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.AddClaimAsync(id, "department");

        result.Status.Should().Be(AddApiScopeClaimStatus.AlreadyApplied);
        result.ClaimType.Should().Be("department");
    }

    [Fact]
    public async Task AddClaimAsync_Success_TrimmedClaimAdded_UpdatesTimestamp()
    {
        int id;
        DateTime? originalUpdated;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                Enabled = true,
                Updated = DateTime.UtcNow.AddDays(-1)
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
            originalUpdated = apiScope.Updated;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.AddClaimAsync(id, "  department  ");

        result.Status.Should().Be(AddApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("department");

        var updated = await configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .SingleAsync(scope => scope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("department");
        updated.Updated.Should().HaveValue();
        updated.Updated!.Value.Should().BeAfter(originalUpdated!.Value);
    }

    [Fact]
    public async Task RemoveClaimAsync_MissingScope_ReturnsNotFound()
    {
        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.RemoveClaimAsync(999, "department");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotFound);
    }

    [Fact]
    public async Task RemoveClaimAsync_NotApplied_ReturnsNotApplied()
    {
        int id;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                Enabled = true,
                UserClaims = new List<ApiScopeClaim>
                {
                    new() { Type = "location" }
                }
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.RemoveClaimAsync(id, "department");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.NotApplied);
        result.ClaimType.Should().Be("department");
    }

    [Fact]
    public async Task RemoveClaimAsync_Success_RemovesClaim_UpdatesTimestamp()
    {
        int id;
        DateTime? originalUpdated;
        using (var seedConfigurationDbContext = CreateConfigurationContext())
        {
            var apiScope = new ApiScope
            {
                Name = "orders.read",
                Enabled = true,
                Updated = DateTime.UtcNow.AddDays(-1),
                UserClaims = new List<ApiScopeClaim>
                {
                    new() { Type = "department" },
                    new() { Type = "location" }
                }
            };
            seedConfigurationDbContext.ApiScopes.Add(apiScope);
            await seedConfigurationDbContext.SaveChangesAsync();
            id = apiScope.Id;
            originalUpdated = apiScope.Updated;
        }

        using var configurationDbContext = CreateConfigurationContext();
        using var applicationDbContext = CreateApplicationContext();
        var service = CreateService(configurationDbContext, applicationDbContext);

        var result = await service.RemoveClaimAsync(id, "  department  ");

        result.Status.Should().Be(RemoveApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("department");

        var updated = await configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .SingleAsync(scope => scope.Id == id);
        updated.UserClaims.Select(claim => claim.Type).Should().NotContain("department");
        updated.UserClaims.Select(claim => claim.Type).Should().Contain("location");
        updated.Updated.Should().HaveValue();
        updated.Updated!.Value.Should().BeAfter(originalUpdated!.Value);
    }
}
