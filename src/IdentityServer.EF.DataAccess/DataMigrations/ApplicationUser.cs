// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Identity;

namespace IdentityServerAspNetIdentity.Models;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    // make the property nullable
    public string? FavoriteColor { get; set; }
}





public class ApplicationUserWithClaims
{
    private ApplicationUser applicationUser;
    private List<KeyValuePair<string, string>> userUserClaims;
    private List<string> userRoles;

    public ApplicationUserWithClaims(List<string> userRoles)
    {
        this.userRoles = userRoles;
        userUserClaims = new List<KeyValuePair<string, string>>();
    }

    public ApplicationUserWithClaims(ApplicationUser applicationUser, List<KeyValuePair<string, string>> userUserClaims, List<string> userRoles)
    {
        this.applicationUser = applicationUser;
        this.userUserClaims = userUserClaims;
        this.userRoles = userRoles;
    }

    public ApplicationUser User
    {
        get => applicationUser;
        set => applicationUser = value;
    }

    public List<KeyValuePair<string, string>> UserClaims
    {
        get => userUserClaims;
        set => userUserClaims = value;
    }
    
    public List<string> UserRoles
    {
        get => userRoles;
        set => userRoles = value;
    }
}

