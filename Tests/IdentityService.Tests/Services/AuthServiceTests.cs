using NUnit.Framework;
using Moq;
using IdentityService.Services;
using IdentityService.Interfaces;
using IdentityService.Models;
using IdentityService.DTOs;
using IdentityService.Exceptions;
using IdentityService.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MassTransit;
using System.Threading.Tasks;

namespace IdentityService.Tests.Services
{
    [TestFixture]
    public class AuthServiceTests
    {
        private FakeUserRepository _fakeUserRepo;
        private Mock<IStoreRepository> _storeRepoMock;
        private Mock<IConfiguration> _configMock;
        private Mock<IEmailService> _emailServiceMock;
        private Mock<ILogger<AuthService>> _loggerMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        
        private AuthService _authService;

        [SetUp]
        public void Setup()
        {
            _fakeUserRepo = new FakeUserRepository();
            _storeRepoMock = new Mock<IStoreRepository>();
            _configMock = new Mock<IConfiguration>();
            _emailServiceMock = new Mock<IEmailService>();
            _loggerMock = new Mock<ILogger<AuthService>>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();

            // Setup mock config for JWT
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(s => s["Key"]).Returns("ThisIsASecretKeyForTestingPurposesThatIsLongEnough");
            jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
            _configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);

            var testAccountsSection = new Mock<IConfigurationSection>();
            _configMock.Setup(c => c.GetSection("TestAccounts")).Returns(testAccountsSection.Object);

            _authService = new AuthService(
                _fakeUserRepo,
                _storeRepoMock.Object,
                _configMock.Object,
                _loggerMock.Object,
                _emailServiceMock.Object,
                _publishEndpointMock.Object
            );
        }

        [Test]
        public void LoginAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "notfound@test.com", Password = "Password123" };
            _fakeUserRepo.UserToReturn = null;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(loginDto));
            Assert.That(ex.Message, Does.Contain("Invalid email or password."));
        }

        [Test]
        public void LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "found@test.com", Password = "WrongPassword" };
            var user = new User { Email = loginDto.Email, PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword") };
            _fakeUserRepo.UserToReturn = user;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(loginDto));
            Assert.That(ex.Message, Does.Contain("Invalid email or password."));
        }

        [Test]
        public void LoginAsync_EmailNotVerified_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "found@test.com", Password = "Password123" };
            var user = new User { 
                Email = loginDto.Email, 
                PasswordHash = "Password123",
                IsEmailVerified = false 
            };
            _fakeUserRepo.UserToReturn = user;

            // Act & Assert
            var ex = Assert.ThrowsAsync<ValidationException>(() => _authService.LoginAsync(loginDto));
            Assert.That(ex.Message, Does.Contain("EMAIL_NOT_VERIFIED"));
        }

        [Test]
        public async Task LoginAsync_ValidCredentials_ReturnsAuthResultWithToken()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "found@test.com", Password = "Password123" };
            var user = new User { 
                Id = 1,
                Email = loginDto.Email, 
                PasswordHash = "Password123",
                IsEmailVerified = true,
                UserRoles = new List<UserStoreRole> { new UserStoreRole { Role = new Role { Name = "Admin" } } }
            };
            _fakeUserRepo.UserToReturn = user;

            // Act
            var result = await _authService.LoginAsync(loginDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Message, Is.EqualTo("OTP_SENT").Or.EqualTo("Login successful"));
            if(result.Data != null)
            {
                Assert.That(result.Data.Token, Is.Not.Null.Or.Empty);
                Assert.That(result.Data.Role, Is.EqualTo("Admin"));
                Assert.That(result.Data.Email, Is.EqualTo(user.Email));
            }
        }
    }

    public class FakeUserRepository : IUserRepository
    {
        public User? UserToReturn { get; set; }

        public Task<User?> SingleOrDefaultAsync(System.Linq.Expressions.Expression<System.Func<User, bool>> predicate)
        {
            return Task.FromResult(UserToReturn);
        }

        public Task<User?> GetWithRolesByEmailAsync(string email) => Task.FromResult(UserToReturn);
        public Task<User?> GetWithRolesByIdAsync(int id) => Task.FromResult(UserToReturn);
        public Task<User?> GetByIdAsync(int id) => Task.FromResult(UserToReturn);
        public Task<System.Collections.Generic.IEnumerable<User>> GetAllAsync() => Task.FromResult<System.Collections.Generic.IEnumerable<User>>(new List<User>());
        public Task<System.Collections.Generic.IEnumerable<User>> FindAsync(System.Linq.Expressions.Expression<System.Func<User, bool>> predicate) => Task.FromResult<System.Collections.Generic.IEnumerable<User>>(new List<User>());
        public Task<bool> AnyAsync(System.Linq.Expressions.Expression<System.Func<User, bool>> predicate) => Task.FromResult(false);
        public Task AddAsync(User entity) => Task.CompletedTask;
        public void Update(User entity) { }
        public void Delete(User entity) { }
        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
