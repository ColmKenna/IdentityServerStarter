using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using FluentAssertions;
using IdentityServerAspNetIdentity.TestSupport.Infrastructure;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerAspNetIdentity.IntegrationTests.Services;

public class ApiScopesAdminServiceTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private IServiceScope _scope = default!;
    private ConfigurationDbContext _configDbContext = default!;
    private IApiScopesAdminService _sut = default!;

    public ApiScopesAdminServiceTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();
        _configDbContext = _scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IApiScopesAdminService>();
        
        // Ensure clean state per test since InMemory database is shared across tests within the same class fixture lifecycle.
        _configDbContext.ApiScopes.RemoveRange(_configDbContext.ApiScopes);
        return _configDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _scope.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistScope_WhenNameIsUnique()
    {
        // Arrange
        var request = new CreateApiScopeRequest { Name = "new_scope", DisplayName = "New Scope", Enabled = true };

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Status.Should().Be(CreateApiScopeStatus.Success);
        result.CreatedId.Should().BeGreaterThan(0);

        var savedScope = await _configDbContext.ApiScopes.FindAsync(result.CreatedId);
        savedScope.Should().NotBeNull();
        savedScope!.Name.Should().Be("new_scope");
        savedScope.ShowInDiscoveryDocument.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnDuplicateName_WhenNameAlreadyExists()
    {
        // Arrange
        _configDbContext.ApiScopes.Add(new ApiScope { Name = "existing_scope" });
        await _configDbContext.SaveChangesAsync();

        var request = new CreateApiScopeRequest { Name = "existing_scope " }; // Note trailing space to test trim logic

        // Act
        var result = await _sut.CreateAsync(request);

        // Assert
        result.Status.Should().Be(CreateApiScopeStatus.DuplicateName);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity_WhenIdExistsAndNameUnique()
    {
        // Arrange
        var scope = new ApiScope { Name = "old_name", DisplayName = "Old Display Name", Enabled = false };
        _configDbContext.ApiScopes.Add(scope);
        await _configDbContext.SaveChangesAsync();

        var request = new UpdateApiScopeRequest { Name = "new_name", DisplayName = "New Display Name", Enabled = true };

        // Act
        var result = await _sut.UpdateAsync(scope.Id, request);

        // Assert
        result.Status.Should().Be(UpdateApiScopeStatus.Success);
        
        var updatedScope = await _configDbContext.ApiScopes.FindAsync(scope.Id);
        updatedScope!.Name.Should().Be("new_name");
        updatedScope.DisplayName.Should().Be("New Display Name");
        updatedScope.Enabled.Should().BeTrue();
    }

    [Fact]
    public async Task AddClaimAsync_ShouldAddNewClaim_WhenNotAlreadyApplied()
    {
        // Arrange
        var scope = new ApiScope { Name = "test_scope" };
        _configDbContext.ApiScopes.Add(scope);
        await _configDbContext.SaveChangesAsync();

        // Act
        var result = await _sut.AddClaimAsync(scope.Id, "email");

        // Assert
        result.Status.Should().Be(AddApiScopeClaimStatus.Success);
        result.ClaimType.Should().Be("email");

        var updatedScope = await _configDbContext.ApiScopes.FindAsync(scope.Id);
        await _configDbContext.Entry(updatedScope!).Collection(s => s.UserClaims).LoadAsync();
        updatedScope!.UserClaims.Should().ContainSingle(c => c.Type == "email");
    }
}
