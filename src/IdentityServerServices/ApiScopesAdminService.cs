using System.Linq.Expressions;
using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using EfCoreExtensions;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;
using PrimativeExtensions;

namespace IdentityServerServices;

public class ApiScopesAdminService : IApiScopesAdminService
{
    private readonly ConfigurationDbContext _configurationDbContext;
    private readonly ApplicationDbContext _applicationDbContext;

    public ApiScopesAdminService(
        ConfigurationDbContext configurationDbContext,
        ApplicationDbContext applicationDbContext)
    {
        _configurationDbContext = configurationDbContext;
        _applicationDbContext = applicationDbContext;
    }


    private static Expression<Func<ApiScope, ApiScopeListItemDto>> MapToListItemDto => scope => new ApiScopeListItemDto
    {
        Id = scope.Id,
        Name = scope.Name,
        DisplayName = scope.DisplayName ?? string.Empty,
        Description = scope.Description ?? string.Empty,
        Enabled = scope.Enabled
    };

    private static ApiScopeEditPageDataDto CreateApiScopeEditPageData(List<string> allUserClaims)
    {
        return new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = string.Empty,
                Enabled = true
            },
            AppliedUserClaims = Array.Empty<string>(),
            AvailableUserClaims = allUserClaims
        };
    }



    public Task<IReadOnlyList<ApiScopeListItemDto>> GetApiScopesAsync(CancellationToken cancellationToken = default)
    {
        return _configurationDbContext.ApiScopes
            .AsNoTracking()
            .OrderBy(scope => scope.Name)
            .Select(MapToListItemDto)
            .ToReadOnlyListAsync(cancellationToken);
    }



    public async Task<ApiScopeEditPageDataDto> GetForCreateAsync(CancellationToken cancellationToken = default)
    {
        var allUserClaims = 
            await GetAllUserClaimTypesAsync(cancellationToken);
        return CreateApiScopeEditPageData(allUserClaims);
    }

    public async Task<ApiScopeEditPageDataDto?> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .AsNoTracking()
            .FirstOrDefaultAsync(scope => scope.Id == id, cancellationToken);

        if (apiScope is null)
        {
            return null;
        }

        return await BuildPageDataAsync(apiScope, cancellationToken);
    }

    private static ApiScope ToApiScope(CreateApiScopeRequest input)
    {
        return new ApiScope
        {
            Name = input.Name.Trim(),
            DisplayName = NormalizeOptional(input.DisplayName),
            Description = NormalizeOptional(input.Description),
            Enabled = input.Enabled,
            ShowInDiscoveryDocument = true,
            Required = false,
            Emphasize = false,
            NonEditable = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };
    }

    private Task<bool> DuplicateNameExists(string normalizedName, CancellationToken cancellationToken)
    {
        return _configurationDbContext.ApiScopes
            .AnyAsync(scope => scope.Name == normalizedName , cancellationToken);
    }

    public async Task<CreateApiScopeResult> CreateAsync(CreateApiScopeRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedName = request.Name.Trim();
        var duplicateNameExists = await DuplicateNameExists(normalizedName, cancellationToken);

        if (duplicateNameExists)
        {
            return CreateApiScopeResult.DuplicateName();
        }
        var entity = ToApiScope(request);
        _configurationDbContext.ApiScopes.Add(entity);
        await _configurationDbContext.SaveChangesAsync(cancellationToken);

        return CreateApiScopeResult.Success(entity.Id);
    }

    public async Task<UpdateApiScopeResult> UpdateAsync(int id, UpdateApiScopeRequest request, CancellationToken cancellationToken = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .FirstOrDefaultAsync(scope => scope.Id == id, cancellationToken);

        if (apiScope is null)
        {
            return UpdateApiScopeResult.NotFound();
        }

        var normalizedName = request.Name.Trim();
        var duplicateNameExists = await DuplicateNameExists(id, normalizedName, cancellationToken);

        if (duplicateNameExists)
        {
            return UpdateApiScopeResult.DuplicateName();
        }

        apiScope.Name = normalizedName;
        apiScope.DisplayName = NormalizeOptional(request.DisplayName);
        apiScope.Description = NormalizeOptional(request.Description);
        apiScope.Enabled = request.Enabled;
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(cancellationToken);
        return UpdateApiScopeResult.Success();
    }

    private Task<bool> DuplicateNameExists(int id, string normalizedName, CancellationToken cancellationToken)
    {
        return _configurationDbContext.ApiScopes
            .AnyAsync(scope => scope.Name == normalizedName && scope.Id != id, cancellationToken);
    }


    public async Task<AddApiScopeClaimResult> AddClaimAsync(int id, string claimType, CancellationToken cancellationToken = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .FirstOrDefaultAsync(scope => scope.Id == id, cancellationToken);

        if (apiScope is null)
        {
            return AddApiScopeClaimResult.NotFound();
        }

        var normalizedClaimType = claimType.Trim();
        if (apiScope.UserClaims.Any(claim => claim.Type == normalizedClaimType))
        {
            return AddApiScopeClaimResult.AlreadyApplied(normalizedClaimType);
        }

        apiScope.UserClaims.Add(new ApiScopeClaim
        {
            Type = normalizedClaimType
        });
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(cancellationToken);
        return AddApiScopeClaimResult.Success(normalizedClaimType);
    }

    public async Task<RemoveApiScopeClaimResult> RemoveClaimAsync(int id, string claimType, CancellationToken cancellationToken = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .FirstOrDefaultAsync(scope => scope.Id == id, cancellationToken);

        if (apiScope is null)
        {
            return RemoveApiScopeClaimResult.NotFound();
        }

        var normalizedClaimType = claimType.Trim();
        var claim = apiScope.UserClaims.FirstOrDefault(existingClaim => existingClaim.Type == normalizedClaimType);
        if (claim is null)
        {
            return RemoveApiScopeClaimResult.NotApplied(normalizedClaimType);
        }

        apiScope.UserClaims.Remove(claim);
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(cancellationToken);
        return RemoveApiScopeClaimResult.Success(normalizedClaimType);
    }

    private async Task<ApiScopeEditPageDataDto> BuildPageDataAsync(ApiScope apiScope, CancellationToken cancellationToken)
    {
        var appliedClaims = apiScope.UserClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Type))
            .Select(claim => claim.Type.Trim())
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToList();

        var allUserClaimTypes = await GetAllUserClaimTypesAsync(cancellationToken);
        var availableClaims = allUserClaimTypes
            .Where(claimType => !appliedClaims.Contains(claimType))
            .ToList();

        return new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = apiScope.Name,
                DisplayName = apiScope.DisplayName,
                Description = apiScope.Description,
                Enabled = apiScope.Enabled
            },
            AppliedUserClaims = appliedClaims,
            AvailableUserClaims = availableClaims
        };
    }

    private Task<List<string>> GetAllUserClaimTypesAsync(CancellationToken cancellationToken)
    {
        return _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType != null && claim.ClaimType != string.Empty)
            .Select(claim => claim.ClaimType!)
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToListAsync(cancellationToken);
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
