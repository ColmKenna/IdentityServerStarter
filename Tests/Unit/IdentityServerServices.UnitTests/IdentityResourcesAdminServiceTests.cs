using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using Duende.IdentityServer.EntityFramework.Options;
using FluentAssertions;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class IdentityResourcesAdminServiceTests
{
    private static ConfigurationDbContext CreateDbContext()
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

    [Fact]
    public async Task GetIdentityResourcesAsync_ShouldReturnEmptyList_WhenNoResourcesExist()
    {
        await using var context = CreateDbContext();
        var sut = new IdentityResourcesAdminService(context);

        var result = await sut.GetIdentityResourcesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ShouldReturnMappedDtos_SortedByName()
    {
        await using var context = CreateDbContext();
        context.IdentityResources.AddRange(
            new IdentityResource { Name = "Profile", DisplayName = "User Profile", Description = "Profile data", Enabled = true },
            new IdentityResource { Name = "Address", DisplayName = null, Description = null, Enabled = false },
            new IdentityResource { Name = "Email", DisplayName = "Email Address", Description = "Email data", Enabled = true }
        );
        await context.SaveChangesAsync();
        var sut = new IdentityResourcesAdminService(context);

        var result = await sut.GetIdentityResourcesAsync();

        result.Should().HaveCount(3);
        result.Select(r => r.Name).Should().BeInAscendingOrder();
        
        var addressResource = result.Single(r => r.Name == "Address");
        addressResource.DisplayName.Should().Be(string.Empty);
        addressResource.Description.Should().Be(string.Empty);
        addressResource.Enabled.Should().BeFalse();
    }
}
