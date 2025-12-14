using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HabitRPG.Api.Data;
using HabitRPG.Api.Models;
using HabitRPG.Api.Services;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Tests.Helpers;

namespace HabitRPG.Api.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<ILogger<AuthService>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _loggerMock = new Mock<ILogger<AuthService>>();
            _configMock = new Mock<IConfiguration>();

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Issuer"]).Returns("HabitRPG.Api");
            jwtSection.Setup(x => x["Audience"]).Returns("HabitRPG.Mobile");
            jwtSection.Setup(x => x["ExpirationHours"]).Returns("24");

            _configMock.Setup(x => x.GetSection("JwtSettings")).Returns(jwtSection.Object);
            
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

            _context = TestHelpers.CreateInMemoryContext();
            _authService = new AuthService(_context, _configMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterAsync_ValidRequest_ReturnsSuccess()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeTrue();
            result.User.Should().NotBeNull();
            result.User!.Username.Should().Be("testuser");
            result.User.Email.Should().Be("test@example.com");
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task RegisterAsync_EmptyUsername_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Username is required");
        }

        [Fact]
        public async Task RegisterAsync_ShortUsername_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "ab", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("between 3 and 50 characters");
        }

        [Fact]
        public async Task RegisterAsync_LongUsername_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: new string('a', 51), email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("between 3 and 50 characters");
        }

        [Fact]
        public async Task RegisterAsync_InvalidEmail_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "invalid-email", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("valid email address");
        }

        [Fact]
        public async Task RegisterAsync_EmptyEmail_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Email is required");
        }

        [Fact]
        public async Task RegisterAsync_ShortPassword_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "12345");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("at least 6 characters");
        }

        [Fact]
        public async Task RegisterAsync_LongPassword_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: new string('a', 101));

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("cannot exceed 100 characters");
        }

        [Fact]
        public async Task RegisterAsync_EmptyPassword_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Be("Password is required");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsFailure()
        {
            var existingUser = TestHelpers.CreateTestUser(id: 1, username: "existing", email: "test@example.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("email already exists");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmailCaseInsensitive_ReturnsFailure()
        {
            var existingUser = TestHelpers.CreateTestUser(id: 1, username: "existing", email: "Test@Example.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("email already exists");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ReturnsFailure()
        {
            var existingUser = TestHelpers.CreateTestUser(id: 1, username: "testuser", email: "existing@example.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("username is already taken");
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsernameCaseInsensitive_ReturnsFailure()
        {
            var existingUser = TestHelpers.CreateTestUser(id: 1, username: "TestUser", email: "existing@example.com");
            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var request = TestHelpers.CreateTestRegisterRequest(username: "testuser", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("username is already taken");
        }

        [Fact]
        public async Task RegisterAsync_InvalidUsernameCharacters_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "test user!", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("letters, numbers, hyphens, and underscores");
        }

        [Fact]
        public async Task RegisterAsync_UsernameWithSpecialCharacters_ReturnsFailure()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "test@user", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeFalse();
            result.Message.Should().Contain("letters, numbers, hyphens, and underscores");
        }

        [Fact]
        public async Task RegisterAsync_ValidUsernameWithUnderscore_ReturnsSuccess()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "test_user", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeTrue();
            result.User!.Username.Should().Be("test_user");
        }

        [Fact]
        public async Task RegisterAsync_ValidUsernameWithHyphen_ReturnsSuccess()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "test-user", email: "test@example.com", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeTrue();
            result.User!.Username.Should().Be("test-user");
        }

        [Fact]
        public async Task RegisterAsync_TrimsUsernameAndEmail_ReturnsSuccess()
        {
            var request = TestHelpers.CreateTestRegisterRequest(username: "  testuser  ", email: "  TEST@EXAMPLE.COM  ", password: "password123");

            var result = await _authService.RegisterAsync(request);

            result.Success.Should().BeTrue();
            result.User!.Username.Should().Be("testuser");
            result.User.Email.Should().Be("test@example.com");
        }
    }
}