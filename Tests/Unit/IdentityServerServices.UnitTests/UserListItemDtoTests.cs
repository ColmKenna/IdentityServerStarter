using FluentAssertions;
using IdentityServerServices.ViewModels;
using Xunit;

namespace IdentityServerServices.UnitTests;

public class UserListItemDtoTests
{
    [Fact]
    public void LockoutStatus_ShouldReturnDisabled_WhenLockoutEndIsMaxValue()
    {
        var dto = new UserListItemDto { LockoutEnd = DateTimeOffset.MaxValue };

        dto.LockoutStatus.Should().Be("Disabled");
    }

    [Fact]
    public void LockoutStatus_ShouldReturnLockedOut_WhenLockoutEndIsFutureDate()
    {
        var dto = new UserListItemDto { LockoutEnd = DateTimeOffset.UtcNow.AddHours(1) };

        dto.LockoutStatus.Should().Be("Locked Out");
    }

    [Fact]
    public void LockoutStatus_ShouldReturnActive_WhenLockoutEndIsNull()
    {
        var dto = new UserListItemDto { LockoutEnd = null };

        dto.LockoutStatus.Should().Be("Active");
    }

    [Fact]
    public void LockoutStatus_ShouldReturnActive_WhenLockoutEndIsInThePast()
    {
        var dto = new UserListItemDto { LockoutEnd = DateTimeOffset.UtcNow.AddHours(-1) };

        dto.LockoutStatus.Should().Be("Active");
    }
}
