using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using FluentAssertions;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class IdentityResourcesAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ConfigurationDbContext _configDbContext = default!;
    private IIdentityResourcesAdminService _sut = default!;

    public IdentityResourcesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _configDbContext = _scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IIdentityResourcesAdminService>();

        _configDbContext.IdentityResources.RemoveRange(_configDbContext.IdentityResources);
        return _configDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ShouldReturnSortedDtos_WhenResourcesExist()
    {
        // Arrange
        _configDbContext.IdentityResources.AddRange(
            new IdentityResource { Name = "Z_Resource", DisplayName = "Z" },
            new IdentityResource { Name = "A_Resource", DisplayName = "A" }
        );
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetIdentityResourcesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A_Resource");
        result[1].Name.Should().Be("Z_Resource");
    }

    [Fact]
    public async Task GetIdentityResourcesAsync_ShouldReturnEmpty_WhenNoResourcesExist()
    {
        var result = await _sut.GetIdentityResourcesAsync();
        result.Should().BeEmpty();
    }

    // ─── GetForEditAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetForEditAsync_ShouldReturnPageData_WhenResourceExists()
    {
        // Arrange
        var resource = new IdentityResource
        {
            Name = "email",
            DisplayName = "Email",
            Enabled = true,
            UserClaims = new List<IdentityResourceClaim> { new() { Type = "email" } }
        };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetForEditAsync(resource.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Input.Name.Should().Be("email");
        result.Input.DisplayName.Should().Be("Email");
        result.Input.Enabled.Should().BeTrue();
        result.AppliedUserClaims.Should().ContainSingle(c => c == "email");
    }

    [Fact]
    public async Task GetForEditAsync_ShouldReturnNull_WhenResourceDoesNotExist()
    {
        var result = await _sut.GetForEditAsync(99999);
        result.Should().BeNull();
    }

    // ─── UpdateAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity_WhenIdExistsAndNameUnique()
    {
        // Arrange
        var resource = new IdentityResource { Name = "old_name", DisplayName = "Old Name", Enabled = false };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        var request = new UpdateIdentityResourceRequest { Name = "new_name", DisplayName = "New Name", Enabled = true };

        // Act
        var result = await _sut.UpdateAsync(resource.Id, request);

        // Assert
        result.Status.Should().Be(UpdateIdentityResourceStatus.Success);

        var updated = await _configDbContext.IdentityResources.FindAsync(resource.Id);
        updated!.Name.Should().Be("new_name");
        updated.DisplayName.Should().Be("New Name");
        updated.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNotFound_WhenIdDoesNotExist()
    {
        var request = new UpdateIdentityResourceRequest { Name = "email" };
        var result = await _sut.UpdateAsync(99999, request);
        result.Status.Should().Be(UpdateIdentityResourceStatus.NotFound);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnDuplicateName_WhenAnotherResourceHasSameName()
    {
        // Arrange
        _configDbContext.IdentityResources.AddRange(
            new IdentityResource { Name = "email" },
            new IdentityResource { Name = "profile" }
        );
        await _configDbContext.SaveChangesAsync();
        var profileId = _configDbContext.IdentityResources.Single(r => r.Name == "profile").Id;

        var request = new UpdateIdentityResourceRequest { Name = "email " }; // trailing space to test trim
        var result = await _sut.UpdateAsync(profileId, request);

        result.Status.Should().Be(UpdateIdentityResourceStatus.DuplicateName);
    }

    // ─── AddClaimAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddClaimAsync_ShouldAddNewClaim_WhenNotAlreadyApplied()
    {
        // Arrange
        var resource = new IdentityResource { Name = "email" };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AddClaimAsync(resource.Id, "email");

        // Assert
        result.Status.Should().Be(AddIdentityResourceClaimStatus.Success);
        result.ClaimType.Should().Be("email");

        var updated = await _configDbContext.IdentityResources.FindAsync(resource.Id);
        await _configDbContext.Entry(updated!).Collection(r => r.UserClaims).LoadAsync();
        updated!.UserClaims.Should().ContainSingle(c => c.Type == "email");
    }

    [Fact]
    public async Task AddClaimAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        var result = await _sut.AddClaimAsync(99999, "email");
        result.Status.Should().Be(AddIdentityResourceClaimStatus.NotFound);
    }

    [Fact]
    public async Task AddClaimAsync_ShouldReturnAlreadyApplied_WhenClaimAlreadyExists()
    {
        // Arrange
        var resource = new IdentityResource
        {
            Name = "email",
            UserClaims = new List<IdentityResourceClaim> { new() { Type = "email" } }
        };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        var result = await _sut.AddClaimAsync(resource.Id, "email");
        result.Status.Should().Be(AddIdentityResourceClaimStatus.AlreadyApplied);
    }

    // ─── RemoveClaimAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveClaimAsync_ShouldRemoveClaim_WhenClaimIsApplied()
    {
        // Arrange
        var resource = new IdentityResource
        {
            Name = "email",
            UserClaims = new List<IdentityResourceClaim> { new() { Type = "email" } }
        };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.RemoveClaimAsync(resource.Id, "email");

        // Assert
        result.Status.Should().Be(RemoveIdentityResourceClaimStatus.Success);
        result.ClaimType.Should().Be("email");

        var updated = await _configDbContext.IdentityResources.FindAsync(resource.Id);
        await _configDbContext.Entry(updated!).Collection(r => r.UserClaims).LoadAsync();
        updated!.UserClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        var result = await _sut.RemoveClaimAsync(99999, "email");
        result.Status.Should().Be(RemoveIdentityResourceClaimStatus.NotFound);
    }

    [Fact]
    public async Task RemoveClaimAsync_ShouldReturnNotApplied_WhenClaimIsNotOnResource()
    {
        // Arrange
        var resource = new IdentityResource { Name = "email" };
        _configDbContext.IdentityResources.Add(resource);
        await _configDbContext.SaveChangesAsync();

        var result = await _sut.RemoveClaimAsync(resource.Id, "email");
        result.Status.Should().Be(RemoveIdentityResourceClaimStatus.NotApplied);
    }
}
