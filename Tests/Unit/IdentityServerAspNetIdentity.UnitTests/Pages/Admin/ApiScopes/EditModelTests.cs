using IdentityServerAspNetIdentity.Pages.Admin.ApiScopes;
using IdentityServerServices;
using IdentityServerServices.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace IdentityServerAspNetIdentity.UnitTests.Pages.Admin.ApiScopes;

public class EditModelTests
{
    private readonly Mock<IApiScopesAdminService> _mockApiScopesAdminService;
    private readonly Mock<ITempDataDictionary> _mockTempData;
    private readonly EditModel _sut;

    public EditModelTests()
    {
        _mockApiScopesAdminService = new Mock<IApiScopesAdminService>();
        _mockTempData = new Mock<ITempDataDictionary>();

        _sut = new EditModel(_mockApiScopesAdminService.Object)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext()
            },
            TempData = _mockTempData.Object
        };
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageData_WhenInCreateMode()
    {
        // Arrange
        var createData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = string.Empty,
                DisplayName = null,
                Description = null,
                Enabled = true
            },
            AvailableUserClaims = ["sub", "email", "name"],
            AppliedUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(createData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.Name.Should().Be(string.Empty);
        _sut.Input.Enabled.Should().BeTrue();
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["sub", "email", "name"]);
        _sut.AppliedUserClaims.Should().BeEmpty();
    }

    [Fact]
    public async Task OnGetAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        // Arrange
        _sut.Id = 5;

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnGetAsync_ShouldPopulatePageData_WhenScopeExists()
    {
        // Arrange
        _sut.Id = 5;

        var editData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = "existing-scope",
                DisplayName = "Existing Scope",
                Description = "An existing scope",
                Enabled = true
            },
            AppliedUserClaims = ["sub", "email"],
            AvailableUserClaims = ["name", "role"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(editData);

        // Act
        var result = await _sut.OnGetAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.Name.Should().Be("existing-scope");
        _sut.Input.DisplayName.Should().Be("Existing Scope");
        _sut.Input.Description.Should().Be("An existing scope");
        _sut.Input.Enabled.Should().BeTrue();
        _sut.AppliedUserClaims.Should().BeEquivalentTo(["sub", "email"]);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["name", "role"]);
    }

    [Fact]
    public async Task OnPostAsync_ShouldRedirectWithNewId_WhenCreateSucceeds()
    {
        // Arrange
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "new-scope",
            DisplayName = "New Scope",
            Description = "A new scope",
            Enabled = true
        };

        _mockApiScopesAdminService
            .Setup(x => x.CreateAsync(
                It.Is<CreateApiScopeRequest>(r =>
                    r.Name == "new-scope" &&
                    r.DisplayName == "New Scope" &&
                    r.Description == "A new scope" &&
                    r.Enabled == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiScopeResult.Success(42));

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues!["id"].Should().Be(42);
        _mockTempData.VerifySet(t => t["Success"] = "API scope created successfully");
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageAndRepopulateClaims_WhenCreateModelStateInvalid()
    {
        // Arrange
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "",
            DisplayName = "New Scope",
            Description = "A new scope",
            Enabled = true
        };
        _sut.ModelState.AddModelError("Input.Name", "The Name field is required.");

        var createPageData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AvailableUserClaims = ["sub", "email"],
            AppliedUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(createPageData);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.Name.Should().Be("");
        _sut.Input.DisplayName.Should().Be("New Scope");
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["sub", "email"]);
        _sut.AppliedUserClaims.Should().BeEmpty();
        _mockApiScopesAdminService.Verify(
            x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldAddModelErrorAndReturnPage_WhenCreateDuplicateName()
    {
        // Arrange
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "existing-scope",
            DisplayName = "Existing Scope",
            Description = "Already exists",
            Enabled = true
        };

        _mockApiScopesAdminService
            .Setup(x => x.CreateAsync(It.IsAny<CreateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApiScopeResult.DuplicateName());

        var createPageData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AvailableUserClaims = ["sub", "email"],
            AppliedUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForCreateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(createPageData);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState["Input.Name"]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("An API scope with this name already exists.");
        _sut.Input.Name.Should().Be("existing-scope");
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["sub", "email"]);
    }

    // --- OnPostAsync: Edit Mode ---

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenEditScopeDoesNotExist()
    {
        // Arrange
        _sut.Id = 5;

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnPageAndRepopulateClaims_WhenEditModelStateInvalid()
    {
        // Arrange
        _sut.Id = 5;
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "",
            DisplayName = "Updated Scope",
            Description = "Updated description",
            Enabled = true
        };
        _sut.ModelState.AddModelError("Input.Name", "The Name field is required.");

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = "original-scope",
                DisplayName = "Original Scope",
                Description = "Original description",
                Enabled = true
            },
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email", "name"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.Input.Name.Should().Be("");
        _sut.Input.DisplayName.Should().Be("Updated Scope");
        _sut.AppliedUserClaims.Should().BeEquivalentTo(["sub"]);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["email", "name"]);
        _mockApiScopesAdminService.Verify(
            x => x.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnPostAsync_ShouldRedirect_WhenEditUpdateSucceeds()
    {
        // Arrange
        _sut.Id = 5;
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "updated-scope",
            DisplayName = "Updated Scope",
            Description = "Updated description",
            Enabled = false
        };

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.UpdateAsync(5,
                It.Is<UpdateApiScopeRequest>(r =>
                    r.Name == "updated-scope" &&
                    r.DisplayName == "Updated Scope" &&
                    r.Description == "Updated description" &&
                    r.Enabled == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.Success());

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues!["id"].Should().Be(5);
        _mockTempData.VerifySet(t => t["Success"] = "API scope updated successfully");
    }

    [Fact]
    public async Task OnPostAsync_ShouldReturnNotFound_WhenEditUpdateReturnsNotFound()
    {
        // Arrange
        _sut.Id = 5;
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "updated-scope",
            DisplayName = "Updated Scope",
            Description = "Updated description",
            Enabled = true
        };

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = [],
            AvailableUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.UpdateAsync(5, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.NotFound());

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAsync_ShouldAddModelErrorAndReturnPage_WhenEditDuplicateName()
    {
        // Arrange
        _sut.Id = 5;
        _sut.Input = new EditModel.ApiScopeInputModel
        {
            Name = "duplicate-name",
            DisplayName = "Updated Scope",
            Description = "Updated description",
            Enabled = true
        };

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.UpdateAsync(5, It.IsAny<UpdateApiScopeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateApiScopeResult.DuplicateName());

        // Act
        var result = await _sut.OnPostAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState["Input.Name"]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("An API scope with this name already exists.");
        _sut.Input.Name.Should().Be("duplicate-name");
        _sut.AppliedUserClaims.Should().BeEquivalentTo(["sub"]);
        _sut.AvailableUserClaims.Should().BeEquivalentTo(["email"]);
    }

    // --- OnPostAddClaimAsync ---

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        // Arrange
        _sut.Id = 5;

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnPostAddClaimAsync_ShouldAddModelError_WhenSelectedClaimTypeIsEmpty(string? claimType)
    {
        // Arrange
        _sut.Id = 5;
        _sut.SelectedClaimType = claimType!;

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = "test-scope",
                DisplayName = "Test Scope",
                Description = "A test scope",
                Enabled = true
            },
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email", "name"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(EditModel.SelectedClaimType)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Please select a user claim");
        _sut.Input.Name.Should().Be("test-scope");
        _sut.AppliedUserClaims.Should().BeEquivalentTo(["sub"]);
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldReturnNotFound_WhenAddClaimReturnsNotFound()
    {
        // Arrange
        _sut.Id = 5;
        _sut.SelectedClaimType = "email";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = [],
            AvailableUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.AddClaimAsync(5, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.NotFound());

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldAddModelError_WhenClaimAlreadyApplied()
    {
        // Arrange
        _sut.Id = 5;
        _sut.SelectedClaimType = "sub";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto { Name = "test-scope" },
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.AddClaimAsync(5, "sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.AlreadyApplied("sub"));

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(EditModel.SelectedClaimType)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("This user claim is already applied to the API scope.");
    }

    [Fact]
    public async Task OnPostAddClaimAsync_ShouldRedirect_WhenClaimAddedSuccessfully()
    {
        // Arrange
        _sut.Id = 5;
        _sut.SelectedClaimType = "email";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.AddClaimAsync(5, "email", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AddApiScopeClaimResult.Success("email"));

        // Act
        var result = await _sut.OnPostAddClaimAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues!["id"].Should().Be(5);
        _mockTempData.VerifySet(t => t["Success"] = "User claim 'email' added successfully");
    }

    // --- OnPostRemoveClaimAsync ---

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnNotFound_WhenScopeDoesNotExist()
    {
        // Arrange
        _sut.Id = 5;

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApiScopeEditPageDataDto?)null);

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OnPostRemoveClaimAsync_ShouldAddModelError_WhenRemoveClaimTypeIsEmpty(string? claimType)
    {
        // Arrange
        _sut.Id = 5;
        _sut.RemoveClaimType = claimType!;

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto
            {
                Name = "test-scope",
                DisplayName = "Test Scope",
                Description = "A test scope",
                Enabled = true
            },
            AppliedUserClaims = ["sub", "email"],
            AvailableUserClaims = ["name"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(EditModel.RemoveClaimType)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("Please select a user claim to remove");
        _sut.Input.Name.Should().Be("test-scope");
        _sut.AppliedUserClaims.Should().BeEquivalentTo(["sub", "email"]);
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldReturnNotFound_WhenRemoveClaimReturnsNotFound()
    {
        // Arrange
        _sut.Id = 5;
        _sut.RemoveClaimType = "sub";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = [],
            AvailableUserClaims = []
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.RemoveClaimAsync(5, "sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.NotFound());

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldAddModelError_WhenClaimNotApplied()
    {
        // Arrange
        _sut.Id = 5;
        _sut.RemoveClaimType = "role";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto { Name = "test-scope" },
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.RemoveClaimAsync(5, "role", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.NotApplied("role"));

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        result.Should().BeOfType<PageResult>();
        _sut.ModelState[nameof(EditModel.RemoveClaimType)]!.Errors
            .Should().ContainSingle()
            .Which.ErrorMessage.Should().Be("The selected user claim is not applied to this API scope.");
    }

    [Fact]
    public async Task OnPostRemoveClaimAsync_ShouldRedirect_WhenClaimRemovedSuccessfully()
    {
        // Arrange
        _sut.Id = 5;
        _sut.RemoveClaimType = "sub";

        var existingData = new ApiScopeEditPageDataDto
        {
            Input = new ApiScopeEditInputDto(),
            AppliedUserClaims = ["sub"],
            AvailableUserClaims = ["email"]
        };

        _mockApiScopesAdminService
            .Setup(x => x.GetForEditAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingData);

        _mockApiScopesAdminService
            .Setup(x => x.RemoveClaimAsync(5, "sub", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RemoveApiScopeClaimResult.Success("sub"));

        // Act
        var result = await _sut.OnPostRemoveClaimAsync();

        // Assert
        var redirect = result.Should().BeOfType<RedirectToPageResult>().Subject;
        redirect.PageName.Should().Be("/Admin/ApiScopes/Edit");
        redirect.RouteValues!["id"].Should().Be(5);
        _mockTempData.VerifySet(t => t["Success"] = "User claim 'sub' removed successfully");
    }
}
