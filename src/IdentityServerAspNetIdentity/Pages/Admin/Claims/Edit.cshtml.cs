#nullable enable
using System.Security.Claims;
using IdentityServer.EF.DataAccess.DataMigrations;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Pages.Admin.Claims;

[Authorize(Roles = "ADMIN")]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty(SupportsGet = true)]
    public string ClaimType { get; set; } = default!;

    public IList<UserClaimAssignmentItem> UsersInClaim { get; private set; } = new List<UserClaimAssignmentItem>();

    public IList<AvailableUserItem> AvailableUsers { get; private set; } = new List<AvailableUserItem>();

    [BindProperty]
    public string? SelectedUserId { get; set; }

    [BindProperty]
    public string? NewClaimValue { get; set; }

    [BindProperty]
    public string? RemoveUserId { get; set; }

    [BindProperty]
    public string? RemoveClaimValue { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        if (!await ClaimTypeExistsAsync())
        {
            return NotFound();
        }

        await PopulatePageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddUserAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        if (!await ClaimTypeExistsAsync())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(SelectedUserId))
        {
            ModelState.AddModelError(nameof(SelectedUserId), "Please select a user");
            await PopulatePageDataAsync();
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewClaimValue))
        {
            ModelState.AddModelError(nameof(NewClaimValue), "Claim value is required");
            await PopulatePageDataAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(SelectedUserId);
        if (user is null)
        {
            return NotFound();
        }

        var alreadyAssigned = await _context.UserClaims
            .AnyAsync(c => c.UserId == SelectedUserId && c.ClaimType == ClaimType);
        if (alreadyAssigned)
        {
            ModelState.AddModelError(string.Empty, "Selected user already has this claim type");
            await PopulatePageDataAsync();
            return Page();
        }

        var result = await _userManager.AddClaimAsync(user, new Claim(ClaimType, NewClaimValue));
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            await PopulatePageDataAsync();
            return Page();
        }

        TempData["Success"] = $"Claim '{ClaimType}' assigned to user '{user.UserName}'.";
        return RedirectToPage("/Admin/Claims/Edit", new { claimType = ClaimType });
    }

    public async Task<IActionResult> OnPostRemoveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(ClaimType))
        {
            return BadRequest();
        }

        if (!await ClaimTypeExistsAsync())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(RemoveUserId) || RemoveClaimValue is null)
        {
            ModelState.AddModelError(string.Empty, "Claim assignment details are required");
            await PopulatePageDataAsync();
            return Page();
        }

        var user = await _userManager.FindByIdAsync(RemoveUserId);
        if (user is null)
        {
            return NotFound();
        }

        var assignmentExists = await _context.UserClaims.AnyAsync(c =>
            c.UserId == RemoveUserId &&
            c.ClaimType == ClaimType &&
            (c.ClaimValue ?? string.Empty) == RemoveClaimValue);
        if (!assignmentExists)
        {
            ModelState.AddModelError(string.Empty, "The selected claim assignment does not exist");
            await PopulatePageDataAsync();
            return Page();
        }

        var result = await _userManager.RemoveClaimAsync(user, new Claim(ClaimType, RemoveClaimValue));
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            await PopulatePageDataAsync();
            return Page();
        }

        var hasRemainingAssignments = await ClaimTypeExistsAsync();
        if (!hasRemainingAssignments)
        {
            TempData["Warning"] = $"Claim type '{ClaimType}' no longer has any assigned users and was removed from the system. Assign it to a user with a different value to keep this claim.";
            return RedirectToPage("/Admin/Claims/Index");
        }

        TempData["Success"] = $"Claim '{ClaimType}' removed from user '{user.UserName}'.";
        return RedirectToPage("/Admin/Claims/Edit", new { claimType = ClaimType });
    }

    private async Task<bool> ClaimTypeExistsAsync()
    {
        return await _context.UserClaims.AnyAsync(c => c.ClaimType == ClaimType);
    }

    private async Task PopulatePageDataAsync()
    {
        var assignments = await _context.UserClaims
            .AsNoTracking()
            .Where(c => c.ClaimType == ClaimType)
            .Select(c => new
            {
                c.UserId,
                ClaimValue = c.ClaimValue ?? string.Empty
            })
            .ToListAsync();

        if (ShouldDefaultNewClaimValue(assignments.Select(a => a.ClaimValue)))
        {
            NewClaimValue = bool.TrueString.ToLowerInvariant();
        }

        var users = _userManager.Users.ToList();
        var usersById = users.ToDictionary(u => u.Id, u => u);

        UsersInClaim = assignments
            .Where(a => usersById.ContainsKey(a.UserId))
            .Select(a => new UserClaimAssignmentItem
            {
                UserId = a.UserId,
                UserName = usersById[a.UserId].UserName ?? string.Empty,
                Email = usersById[a.UserId].Email ?? string.Empty,
                ClaimValue = a.ClaimValue
            })
            .OrderBy(a => a.UserName)
            .ThenBy(a => a.ClaimValue)
            .ToList();

        MarkLastUserAssignments();

        var assignedUserIds = UsersInClaim
            .Select(a => a.UserId)
            .ToHashSet(StringComparer.Ordinal);

        AvailableUsers = users
            .Where(u => !assignedUserIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .Select(u => new AvailableUserItem
            {
                UserId = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty
            })
            .ToList();
    }

    private void MarkLastUserAssignments()
    {
        var assignmentCountsByUser = UsersInClaim
            .GroupBy(a => a.UserId)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        var uniqueUsersCount = assignmentCountsByUser.Count;

        foreach (var assignment in UsersInClaim)
        {
            var assignmentCount = assignmentCountsByUser[assignment.UserId];
            assignment.IsLastUserAssignment = uniqueUsersCount == 1 && assignmentCount == 1;
        }
    }

    private bool ShouldDefaultNewClaimValue(IEnumerable<string> claimValues)
    {
        if (!string.IsNullOrWhiteSpace(NewClaimValue))
        {
            return false;
        }

        var normalizedClaimValues = claimValues
            .Select(v => (v ?? string.Empty).Trim())
            .ToList();

        if (normalizedClaimValues.Count == 0)
        {
            return false;
        }

        return normalizedClaimValues.All(v => bool.TryParse(v, out _));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public class UserClaimAssignmentItem
    {
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string ClaimValue { get; set; } = string.Empty;
        public bool IsLastUserAssignment { get; set; }
    }

    public class AvailableUserItem
    {
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string Email { get; set; } = default!;
    }
}
