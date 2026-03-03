using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IReadOnlyList<ApiScopeListItemDto>> GetApiScopesAsync(CancellationToken cancellationToken = default)
    {
        return await _configurationDbContext.ApiScopes
            .AsNoTracking()
            .OrderBy(scope => scope.Name)
            .Select(scope => new ApiScopeListItemDto
            {
                Id = scope.Id,
                Name = scope.Name,
                DisplayName = scope.DisplayName ?? string.Empty,
                Description = scope.Description ?? string.Empty,
                Enabled = scope.Enabled
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ApiScopeEditPageDataDto> GetForCreateAsync(CancellationToken ct = default)
    {
        var availableClaims = await GetAllUserClaimTypesAsync(ct);
        return new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = string.Empty,
                Enabled = true
            },
            AppliedUserClaims = Array.Empty<string>(),
            AvailableUserClaims = availableClaims
        };
    }

    public async Task<ApiScopeEditPageDataDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .AsNoTracking()
            .FirstOrDefaultAsync(scope => scope.Id == id, ct);

        if (apiScope is null)
        {
            return null;
        }

        return await BuildPageDataAsync(apiScope, ct);
    }

    public async Task<CreateApiScopeResult> CreateAsync(CreateApiScopeRequest request, CancellationToken ct = default)
    {
        var normalizedName = request.Name.Trim();
        var duplicateNameExists = await _configurationDbContext.ApiScopes
            .AnyAsync(scope => scope.Name == normalizedName, ct);

        if (duplicateNameExists)
        {
            return new CreateApiScopeResult
            {
                Status = CreateApiScopeStatus.DuplicateName
            };
        }

        var entity = new ApiScope
        {
            Name = normalizedName,
            DisplayName = NormalizeOptional(request.DisplayName),
            Description = NormalizeOptional(request.Description),
            Enabled = request.Enabled,
            ShowInDiscoveryDocument = true,
            Required = false,
            Emphasize = false,
            NonEditable = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        _configurationDbContext.ApiScopes.Add(entity);
        await _configurationDbContext.SaveChangesAsync(ct);

        return new CreateApiScopeResult
        {
            Status = CreateApiScopeStatus.Success,
            CreatedId = entity.Id
        };
    }

    public async Task<UpdateApiScopeResult> UpdateAsync(int id, UpdateApiScopeRequest request, CancellationToken ct = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .FirstOrDefaultAsync(scope => scope.Id == id, ct);

        if (apiScope is null)
        {
            return new UpdateApiScopeResult
            {
                Status = UpdateApiScopeStatus.NotFound
            };
        }

        var normalizedName = request.Name.Trim();
        var duplicateNameExists = await _configurationDbContext.ApiScopes
            .AnyAsync(scope => scope.Name == normalizedName && scope.Id != id, ct);

        if (duplicateNameExists)
        {
            return new UpdateApiScopeResult
            {
                Status = UpdateApiScopeStatus.DuplicateName
            };
        }

        apiScope.Name = normalizedName;
        apiScope.DisplayName = NormalizeOptional(request.DisplayName);
        apiScope.Description = NormalizeOptional(request.Description);
        apiScope.Enabled = request.Enabled;
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return new UpdateApiScopeResult
        {
            Status = UpdateApiScopeStatus.Success
        };
    }

    public async Task<AddApiScopeClaimResult> AddClaimAsync(int id, string claimType, CancellationToken ct = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .FirstOrDefaultAsync(scope => scope.Id == id, ct);

        if (apiScope is null)
        {
            return new AddApiScopeClaimResult
            {
                Status = AddApiScopeClaimStatus.NotFound
            };
        }

        var normalizedClaimType = claimType.Trim();
        if (apiScope.UserClaims.Any(claim => claim.Type == normalizedClaimType))
        {
            return new AddApiScopeClaimResult
            {
                Status = AddApiScopeClaimStatus.AlreadyApplied,
                ClaimType = normalizedClaimType
            };
        }

        apiScope.UserClaims.Add(new ApiScopeClaim
        {
            Type = normalizedClaimType
        });
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return new AddApiScopeClaimResult
        {
            Status = AddApiScopeClaimStatus.Success,
            ClaimType = normalizedClaimType
        };
    }

    public async Task<RemoveApiScopeClaimResult> RemoveClaimAsync(int id, string claimType, CancellationToken ct = default)
    {
        var apiScope = await _configurationDbContext.ApiScopes
            .Include(scope => scope.UserClaims)
            .FirstOrDefaultAsync(scope => scope.Id == id, ct);

        if (apiScope is null)
        {
            return new RemoveApiScopeClaimResult
            {
                Status = RemoveApiScopeClaimStatus.NotFound
            };
        }

        var normalizedClaimType = claimType.Trim();
        var claim = apiScope.UserClaims.FirstOrDefault(existingClaim => existingClaim.Type == normalizedClaimType);
        if (claim is null)
        {
            return new RemoveApiScopeClaimResult
            {
                Status = RemoveApiScopeClaimStatus.NotApplied,
                ClaimType = normalizedClaimType
            };
        }

        apiScope.UserClaims.Remove(claim);
        apiScope.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return new RemoveApiScopeClaimResult
        {
            Status = RemoveApiScopeClaimStatus.Success,
            ClaimType = normalizedClaimType
        };
    }

    private async Task<ApiScopeEditPageDataDto> BuildPageDataAsync(ApiScope apiScope, CancellationToken ct)
    {
        var appliedClaims = apiScope.UserClaims
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Type))
            .Select(claim => claim.Type.Trim())
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToList();

        var allUserClaimTypes = await GetAllUserClaimTypesAsync(ct);
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

    private Task<List<string>> GetAllUserClaimTypesAsync(CancellationToken ct)
    {
        return _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(claim => claim.ClaimType != null && claim.ClaimType != string.Empty)
            .Select(claim => claim.ClaimType!)
            .Distinct()
            .OrderBy(claimType => claimType)
            .ToListAsync(ct);
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
