using IdentityServerAspNetIdentity.Pages.Admin.IdentityResources;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.IdentityResources;

public class EditModelTests
{
    private readonly Mock<IIdentityResourcesAdminService> _mockIdentityResourcesAdminService;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockIdentityResourcesAdminService = new Mock<IIdentityResourcesAdminService>();
        var httpContext = new DefaultHttpContext();

        _sut = new EditModel(_mockIdentityResourcesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulateEditModel_WhenEditingExistingResource()
    {
        // Arrange
        int targetId = 42;
        _sut.Id = targetId;

        var expectedData = new IdentityResourceEditPageDataDto
        {
            Input = new IdentityResourceInputModel
            {
                Name = "email",
                DisplayName = "Email",
                Description = "Your email address",
                Enabled = true
            },
            AppliedUserClaims = new[] { "email", "email_verified" },
            AvailableUserClaims = new[] { "name", "role", "website" }
        };

        // We expect this to fail compilation because GetForEditAsync doesn't exist yet!
        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        
        _sut.Input.Name.Should().Be(expectedData.Input.Name);
        _sut.Input.DisplayName.Should().Be(expectedData.Input.DisplayName);
        
        _sut.AppliedUserClaims.Should().BeEquivalentTo(expectedData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(expectedData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenResourceDoesNotExist()
    {
        // Arrange
        _sut.Id = 99;

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IdentityResourceEditPageDataDto?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldRedirectToEditPage_WhenUpdateSucceeds()
    {
        // Arrange
        _sut.Id = 1;
        _sut.Input = new IdentityResourceInputModel
        {
            Name = "updated_name",
            DisplayName = "Updated Name",
            Description = "Updated description",
            Enabled = true
        };

        var existingData = new IdentityResourceEditPageDataDto
        {
            Input = new IdentityResourceInputModel
            {
                Name = "old_name",
                DisplayName = "Old Name",
                Description = "Old description",
                Enabled = true
            },
            AppliedUserClaims = new[] { "email" },
            AvailableUserClaims = new[] { "name", "role" }
        };

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        var expectedResult = new UpdateIdentityResourceResultDto 
        { 
            Status = UpdateIdentityResourceStatus.Success 
        };

        _mockIdentityResourcesAdminService
            .Setup(x => x.UpdateAsync(
                It.Is<int>(id => id == 1), 
                It.Is<UpdateIdentityResourceRequest>(r => r.Name == "updated_name"), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/IdentityResources/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenUpdateReturnsNotFound()
    {
        // Arrange
        _sut.Id = 1;
        _sut.Input = new IdentityResourceInputModel
        {
            Name = "email",
            DisplayName = "Email",
            Enabled = true
        };

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateIdentityResourceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateIdentityResourceResultDto { Status = UpdateIdentityResourceStatus.NotFound });

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageWithModelError_WhenUpdateReturnsDuplicateName()
    {
        // Arrange
        _sut.Id = 1;
        _sut.Input = new IdentityResourceInputModel
        {
            Name = "duplicate_name",
            DisplayName = "Duplicate",
            Enabled = true
        };

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateIdentityResourceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UpdateIdentityResourceResultDto { Status = UpdateIdentityResourceStatus.DuplicateName });

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState["Input.Name"]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("already exists");

        // Claims lists should be re-populated for the page re-render
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageWithoutCallingUpdate_WhenModelStateIsInvalid()
    {
        // Arrange
        _sut.Id = 1;
        _sut.Input = new IdentityResourceInputModel
        {
            Name = "", // Invalid — Name is required
            Enabled = true
        };

        // Simulate ASP.NET model validation failure
        _sut.ModelState.AddModelError("Input.Name", "The Name field is required.");

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();

        // UpdateAsync should NEVER have been called
        _mockIdentityResourcesAdminService.Verify(
            x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateIdentityResourceRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);

        // Claims lists should still be re-populated for the page re-render
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldSetTempDataAndRedirect_OnSuccess()
    {
        // Arrange
        _sut.Id = 1;
        _sut.SelectedClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.AddClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddIdentityResourceClaimResult.Success("email"));

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/IdentityResources/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);

        _sut.TempData["Success"].ToString().Should().Contain("'email' added successfully");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnPageWithModelError_WhenNoClaimSelected()
    {
        // Arrange
        _sut.Id = 1;
        _sut.SelectedClaimType = null;

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState[nameof(_sut.SelectedClaimType)]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("select a user claim");

        // Verify page data re-populated
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound()
    {
        // Arrange
        _sut.Id = 1;
        _sut.SelectedClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.AddClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddIdentityResourceClaimResult.NotFound());

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnPageWithModelError_WhenClaimAlreadyApplied()
    {
        // Arrange
        _sut.Id = 1;
        _sut.SelectedClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.AddClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddIdentityResourceClaimResult.AlreadyApplied("email"));

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState[nameof(_sut.SelectedClaimType)]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("already applied");

        // Verify page data re-populated
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldSetTempDataAndRedirect_OnSuccess()
    {
        // Arrange
        _sut.Id = 1;
        _sut.RemoveClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.RemoveClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveIdentityResourceClaimResult.Success("email"));

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        var redirectResult = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirectResult.PageName.Should().Be("/Admin/IdentityResources/Edit");
        redirectResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(1);

        _sut.TempData["Success"].ToString().Should().Contain("'email' removed successfully");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnPageWithModelError_WhenNoClaimSelected()
    {
        // Arrange
        _sut.Id = 1;
        _sut.RemoveClaimType = null;

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState[nameof(_sut.RemoveClaimType)]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("select a user claim to remove");

        // Verify page data re-populated
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnNotFound_WhenServiceReturnsNotFound()
    {
        // Arrange
        _sut.Id = 1;
        _sut.RemoveClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.RemoveClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveIdentityResourceClaimResult.NotFound());

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnPageWithModelError_WhenClaimNotApplied()
    {
        // Arrange
        _sut.Id = 1;
        _sut.RemoveClaimType = "email";

        var existingData = CreateValidEditPageData();

        _mockIdentityResourcesAdminService
            .Setup(x => x.GetForEditAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockIdentityResourcesAdminService
            .Setup(x => x.RemoveClaimAsync(1, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveIdentityResourceClaimResult.NotApplied("email"));

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState.ErrorCount.Should().Be(1);
        _sut.ModelState[nameof(_sut.RemoveClaimType)]!.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Contain("not applied");

        // Verify page data re-populated
        _sut.AppliedUserClaims.Should().BeEquivalentTo(existingData.AppliedUserClaims);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(existingData.AvailableUserClaims);
    }

    private static IdentityResourceEditPageDataDto CreateValidEditPageData() =>
        new()
        {
            Input = new IdentityResourceInputModel
            {
                Name = "email",
                DisplayName = "Email",
                Description = "Your email address",
                Enabled = true
            },
            AppliedUserClaims = new[] { "email", "email_verified" },
            AvailableUserClaims = new[] { "name", "role", "website" }
        };
}
