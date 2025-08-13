using DataTransferModels;
using IdentityServer.EF.DataAccess;
using IdentityServerAspNetIdentity.Pages.Clients;
using Moq;

namespace IdentityServerAspNetIdentity.Tests;

public class IndexModelTests
{
    private readonly Mock<IClientsRepository> _mockRepository;

    public IndexModelTests()
    {
        _mockRepository = new Mock<IClientsRepository>();
    }

    [Fact]
    public async Task OnGetAsync_PopulatesClientsProperty()
    {
        // Arrange
        var clients = TestData.ClientDtModels();

        _mockRepository.Setup(repo => repo.GetAllClients()).ReturnsAsync(clients);

        var model = new IndexModel(_mockRepository.Object);

        // Act
        await model.OnGetAsync();

        // Assert
        _mockRepository.Verify(repo => repo.GetAllClients(), Times.Once());
        Assert.Equal(clients, model.Clients);
    }
}