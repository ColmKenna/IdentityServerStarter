using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Entities;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerServices.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerServices;

public class IdentityResourcesAdminService(
    ConfigurationDbContext configurationDbContext,
    ApplicationDbContext applicationDbContext) : IIdentityResourcesAdminService
{
    private readonly ConfigurationDbContext _configurationDbContext = configurationDbContext;
    private readonly ApplicationDbContext _applicationDbContext = applicationDbContext;

    public async Task<IReadOnlyList<IdentityResourceListItemDto>> GetIdentityResourcesAsync(CancellationToken ct = default)
    {
        return await _configurationDbContext.IdentityResources
            .AsNoTracking()
            .OrderBy(resource => resource.Name)
            .Select(resource => new IdentityResourceListItemDto
            {
                Id = resource.Id,
                Name = resource.Name,
                DisplayName = resource.DisplayName ?? string.Empty,
                Description = resource.Description ?? string.Empty,
                Enabled = resource.Enabled
            })
            .ToListAsync(ct);
    }

    public async Task<IdentityResourceEditPageDataDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var resource = await _configurationDbContext.IdentityResources
            .Include(r => r.UserClaims)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (resource is null)
            return null;

        return await BuildPageDataAsync(resource, ct);
    }

    public async Task<UpdateIdentityResourceResultDto> UpdateAsync(int id, UpdateIdentityResourceRequest request, CancellationToken ct = default)
    {
        var resource = await _configurationDbContext.IdentityResources
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (resource is null)
            return new UpdateIdentityResourceResultDto { Status = UpdateIdentityResourceStatus.NotFound };

        var normalizedName = request.Name.Trim();
        var duplicateExists = await _configurationDbContext.IdentityResources
            .AnyAsync(r => r.Name == normalizedName && r.Id != id, ct);

        if (duplicateExists)
            return new UpdateIdentityResourceResultDto { Status = UpdateIdentityResourceStatus.DuplicateName };

        resource.Name = normalizedName;
        resource.DisplayName = NormalizeOptional(request.DisplayName);
        resource.Description = NormalizeOptional(request.Description);
        resource.Enabled = request.Enabled;
        resource.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return new UpdateIdentityResourceResultDto { Status = UpdateIdentityResourceStatus.Success };
    }

    public async Task<AddIdentityResourceClaimResult> AddClaimAsync(int id, string claimType, CancellationToken ct = default)
    {
        var resource = await _configurationDbContext.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (resource is null)
            return AddIdentityResourceClaimResult.NotFound();

        var normalizedClaimType = claimType.Trim();
        if (resource.UserClaims.Any(c => c.Type == normalizedClaimType))
            return AddIdentityResourceClaimResult.AlreadyApplied(normalizedClaimType);

        resource.UserClaims.Add(new IdentityResourceClaim { Type = normalizedClaimType });
        resource.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return AddIdentityResourceClaimResult.Success(normalizedClaimType);
    }

    public async Task<RemoveIdentityResourceClaimResult> RemoveClaimAsync(int id, string claimType, CancellationToken ct = default)
    {
        var resource = await _configurationDbContext.IdentityResources
            .Include(r => r.UserClaims)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (resource is null)
            return RemoveIdentityResourceClaimResult.NotFound();

        var normalizedClaimType = claimType.Trim();
        var claim = resource.UserClaims.FirstOrDefault(c => c.Type == normalizedClaimType);
        if (claim is null)
            return RemoveIdentityResourceClaimResult.NotApplied(normalizedClaimType);

        resource.UserClaims.Remove(claim);
        resource.Updated = DateTime.UtcNow;

        await _configurationDbContext.SaveChangesAsync(ct);
        return RemoveIdentityResourceClaimResult.Success(normalizedClaimType);
    }

    private async Task<IdentityResourceEditPageDataDto> BuildPageDataAsync(IdentityResource resource, CancellationToken ct)
    {
        var appliedClaims = resource.UserClaims
            .Where(c => !string.IsNullOrWhiteSpace(c.Type))
            .Select(c => c.Type.Trim())
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var allUserClaimTypes = await _applicationDbContext.UserClaims
            .AsNoTracking()
            .Where(c => !string.IsNullOrEmpty(c.ClaimType))
            .Select(c => c.ClaimType!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync(ct);

        var availableClaims = allUserClaimTypes
            .Where(t => !appliedClaims.Contains(t))
            .ToList();

        return new IdentityResourceEditPageDataDto
        {
            Input = new IdentityResourceInputModel
            {
                Name = resource.Name,
                DisplayName = resource.DisplayName,
                Description = resource.Description,
                Enabled = resource.Enabled
            },
            AppliedUserClaims = appliedClaims,
            AvailableUserClaims = availableClaims
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return value.Trim();
    }
}
