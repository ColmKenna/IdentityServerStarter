using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace IdentityServerAspNetIdentity.UnitTests;

public static class TestHelpers
{
    public static Mock<UserManager<ApplicationUser>> CreateMockUserManager()
    {
        var store = new Mock<IUserStore<ApplicationUser>>();
        var mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        mockUserManager.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
        mockUserManager.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());
        return mockUserManager;
    }
}
