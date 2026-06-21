using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using ResourceMindAI.Application.Abstractions.Repositories;
using ResourceMindAI.Application.DTOs.Auth;
using ResourceMindAI.Application.Services;
using ResourceMindAI.Application.Tests.TestData;
using ResourceMindAI.Domain.Entities;
using ResourceMindAI.Domain.Exceptions;

namespace ResourceMindAI.Application.Tests.Auth;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_users.Object, Mock.Of<ILogger<AuthService>>());
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnUserProfile()
    {
        var user = TestDataBuilder.User();
        _users.Setup(x => x.GetByUsernameAsync(user.Username)).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginDto
        {
            Username = user.Username,
            Password = "Password1"
        });

        result.Id.Should().Be(user.Id);
        result.Username.Should().Be(user.Username);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldThrowUnauthorizedAccessException()
    {
        var user = TestDataBuilder.User();
        _users.Setup(x => x.GetByUsernameAsync(user.Username)).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginDto
        {
            Username = user.Username,
            Password = "WrongPassword1"
        });

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("Invalid username or password.");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsInactive_ShouldThrowForbiddenException()
    {
        var user = TestDataBuilder.User(isActive: false);
        _users.Setup(x => x.GetByUsernameAsync(user.Username)).ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginDto
        {
            Username = user.Username,
            Password = "Password1"
        });

        var exception = await act.Should().ThrowAsync<ForbiddenException>();
        exception.Which.ErrorCode.Should().Be("INACTIVE_ACCOUNT");
    }

    [Fact]
    public async Task ChangePasswordAsync_WithValidPassword_ShouldClearForcePasswordChange()
    {
        var user = TestDataBuilder.User();
        user.ForcePasswordChange = true;
        _users.Setup(x => x.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _users.Setup(x => x.UpdatePasswordAsync(user, It.IsAny<string>()))
            .Callback<User, string>((entity, hash) =>
            {
                entity.PasswordHash = hash;
                entity.ForcePasswordChange = false;
            })
            .ReturnsAsync(user);

        var result = await _sut.ChangePasswordAsync(new ChangePasswordDto
        {
            UserId = user.Id,
            CurrentPassword = "Password1",
            NewPassword = "NewPassword2",
            ConfirmPassword = "NewPassword2"
        });

        result.ForcePasswordChange.Should().BeFalse();
        PasswordHasher.Verify("NewPassword2", user.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_WhenNewPasswordIsWeak_ShouldThrowValidationException()
    {
        var act = () => _sut.ChangePasswordAsync(new ChangePasswordDto
        {
            UserId = Guid.NewGuid(),
            CurrentPassword = "Password1",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        });

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*uppercase letter and a number*");
        _users.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }
}
