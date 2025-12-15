using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HabitRPG.Api.DTOs;
using HabitRPG.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HabitRPG.Api.Tests.Integration
{
    public class AuthControllerTests : IntegrationTestBase
    {
        public AuthControllerTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsSuccess()
        {
            var request = new RegisterRequest
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("message").GetString().Should().Contain("successful");
            result.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
            result.GetProperty("user").GetProperty("username").GetString().Should().Be("newuser");
        }

        [Fact]
        public async Task Register_InvalidEmail_ReturnsBadRequest()
        {
            var request = new RegisterRequest
            {
                Username = "newuser",
                Email = "invalid-email",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_ShortPassword_ReturnsBadRequest()
        {
            var request = new RegisterRequest
            {
                Username = "newuser",
                Email = "newuser@example.com",
                Password = "12345"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsConflict()
        {
            await CreateTestUserAsync(email: "duplicate@example.com");

            var request = new RegisterRequest
            {
                Username = "differentuser",
                Email = "duplicate@example.com",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_DuplicateUsername_ReturnsConflict()
        {
            await CreateTestUserAsync(username: "duplicateuser");

            var request = new RegisterRequest
            {
                Username = "duplicateuser",
                Email = "different@example.com",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);

            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSuccess()
        {
            await CreateTestUserAsync(email: "login@example.com", password: "password123");

            var request = new LoginRequest
            {
                Email = "login@example.com",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
            result.GetProperty("user").GetProperty("email").GetString().Should().Be("login@example.com");
        }

        [Fact]
        public async Task Login_InvalidEmail_ReturnsUnauthorized()
        {
            var request = new LoginRequest
            {
                Email = "nonexistent@example.com",
                Password = "password123"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_InvalidPassword_ReturnsUnauthorized()
        {
            await CreateTestUserAsync(email: "wrongpass@example.com", password: "password123");

            var request = new LoginRequest
            {
                Email = "wrongpass@example.com",
                Password = "wrongpassword"
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_InvalidRequest_ReturnsBadRequest()
        {
            var request = new LoginRequest
            {
                Email = "",
                Password = ""
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}