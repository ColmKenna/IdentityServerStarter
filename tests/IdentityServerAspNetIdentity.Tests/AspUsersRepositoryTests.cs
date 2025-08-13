using IdentityServer.EF.DataAccess;
using IdentityServer.EF.DataAccess.DataMigrations;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Tests;

public class AspUsersRepositoryTests
{
    private readonly ApplicationDbContext _context;
    private readonly AspUsersRepository _repository;

    public AspUsersRepositoryTests()
    {
        // Setup test database (could be an in-memory database for simplicity)
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AspUsersRepository(_context);

        InitializeDatabase(); // Seed the database
    }

    private void InitializeDatabase()
    {
        _context.Users.RemoveRange(_context.Users);
        _context.UserClaims.RemoveRange(_context.UserClaims);
        _context.SaveChanges();

        var users = TestData.Users();

        _context.Users.AddRange(users);
        _context.SaveChanges();


        var userClaims = TestData.IdentityClaims();
        _context.UserClaims.AddRange(userClaims);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsers()
    {
        var users = await _repository.GetAllUsers();

        // Check if the count matches
        Assert.Equal(TestData.Users().Count, users.Count);

        // Validate each user against TestData
        foreach (var expectedUser in TestData.Users())
        {
            var actualUser = users.FirstOrDefault(u => u.Id == expectedUser.Id);

            Assert.NotNull(actualUser);
            Assert.Equal(expectedUser.UserName, actualUser.UserName);
            Assert.Equal(expectedUser.Email, actualUser.Email);
            Assert.Equal(expectedUser.Email, actualUser.Email);

            // Add other property checks if needed
        }
    }
    
    [Fact]
    public async Task GetUser_ReturnsCorrectUser()
    {
        var userWithClaims = await _repository.GetUser("id_johnDoe");  // Assuming "id_johnDoe" was seeded

        // Validate the user based on TestData.Users
        Assert.NotNull(userWithClaims);

        var actualUser = userWithClaims.User;
        
        
        var expectedUser = TestData.Users().FirstOrDefault(u => u.Id == "id_johnDoe");

        Assert.NotNull(expectedUser); // Ensure that expected user is not null

        Assert.Equal(expectedUser.Id, actualUser.Id);
        Assert.Equal(expectedUser.UserName, actualUser.UserName);
        Assert.Equal(expectedUser.Email, actualUser.Email);
        
    }
    
    [Fact]
    public async Task GetUser_ReturnsCorrectUserClaims()
    {
        foreach (var user in TestData.Users())
        {

            var userWithClaims = await _repository.GetUser( user.Id ); // Assuming "id_johnDoe" was seeded

            // Validate the user based on TestData.Users
            Assert.NotNull(userWithClaims);

            var actualClaims = userWithClaims.UserClaims;
            var expectedClaims = TestData.IdentityClaims(user.Id).ToList();

            Assert.NotNull(expectedClaims); // Ensure that expected claims are not null
            Assert.Equal(expectedClaims.Count, actualClaims.Count); // Ensure same number of claims

            foreach (var expectedClaim in expectedClaims)
            {
                var actualClaim = actualClaims.FirstOrDefault(claim =>
                    claim.Key == expectedClaim.ClaimType &&
                    claim.Value == expectedClaim.ClaimValue);

                Assert.NotNull(actualClaim); // Ensure that the expected claim is present in actual claims
            }
        }
    }

}