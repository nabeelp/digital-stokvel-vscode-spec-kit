using DigitalStokvel.Core.DTOs;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using System.Text;
using Xunit;

namespace DigitalStokvel.Tests.Unit.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IDistributedCache> _cacheMock;

    public AuthenticationServiceTests()
    {
        // Mock UserManager (requires complex constructor)
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object,
            null!, null!, null!, null!, null!, null!, null!, null!);

        // Mock SignInManager (requires complex constructor)
        var contextAccessorMock = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        var userPrincipalFactoryMock = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            contextAccessorMock.Object,
            userPrincipalFactoryMock.Object,
            null!, null!, null!, null!);

        _cacheMock = new Mock<IDistributedCache>();
    }

    private AuthenticationService CreateService(JwtTokenService? jwtTokenService = null)
    {
        // Create a real JwtTokenService with mock configuration when needed
        if (jwtTokenService == null)
        {
            var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
            configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
            configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
            configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");

            jwtTokenService = new JwtTokenService(configMock.Object);
        }

        return new AuthenticationService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            jwtTokenService,
            _cacheMock.Object);
    }

    #region RegisterAsync Tests

    [Theory]
    [InlineData("+27821234567")]
    [InlineData("+27123456789")]
    [InlineData("+27823456789")]
    public async Task RegisterAsync_ShouldSucceed_WhenPhoneNumberIsValidInternationalFormat(string phoneNumber)
    {
        // Arrange
        var request = new RegisterRequest(phoneNumber, "Password123!", "EN");
        var expectedUserId = Guid.NewGuid().ToString();

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success)
            .Callback<ApplicationUser, string>((user, password) =>
            {
                user.Id = expectedUserId; // Simulate ID assignment
            });

        var service = CreateService();

        // Act
        var (success, userId, error) = await service.RegisterAsync(request);

        // Assert
        success.Should().BeTrue();
        userId.Should().NotBeNullOrEmpty();
        error.Should().BeNull();

        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u =>
                u.PhoneNumber == phoneNumber &&
                u.UserName == phoneNumber &&
                u.PreferredLanguage == "EN" &&
                u.FicaVerified == false),
            "Password123!"),
            Times.Once);
    }

    [Theory]
    [InlineData("0821234567")]
    [InlineData("0123456789")]
    [InlineData("0823456789")]
    public async Task RegisterAsync_ShouldSucceed_WhenPhoneNumberIsValidLocalFormat(string phoneNumber)
    {
        // Arrange
        var request = new RegisterRequest(phoneNumber, "Password123!", "ZU");

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var (success, userId, error) = await service.RegisterAsync(request);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
    }

    [Theory]
    [InlineData("1234567890")] // Missing country code or 0
    [InlineData("+2782123")] // Too short
    [InlineData("+278212345678")] // Too long
    [InlineData("082123")] // Too short local
    [InlineData("08212345678")] // Too long local
    [InlineData("+44821234567")] // Wrong country code
    [InlineData("27821234567")] // Missing +
    public async Task RegisterAsync_ShouldFail_WhenPhoneNumberIsInvalid(string phoneNumber)
    {
        // Arrange
        var request = new RegisterRequest(phoneNumber, "Password123!", "EN");
        var service = CreateService();

        // Act
        var (success, userId, error) = await service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNull();
        error.Should().Be("Invalid South African phone number format");

        _userManagerMock.Verify(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldFail_WhenUserManagerReturnsErrors()
    {
        // Arrange
        var request = new RegisterRequest("+27821234567", "weak", "EN");
        var errors = new[]
        {
            new IdentityError { Description = "Password is too weak" },
            new IdentityError { Description = "Password requires uppercase letter" }
        };

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(errors));

        var service = CreateService();

        // Act
        var (success, userId, error) = await service.RegisterAsync(request);

        // Assert
        success.Should().BeFalse();
        userId.Should().BeNull();
        error.Should().Contain("Password is too weak");
        error.Should().Contain("Password requires uppercase letter");
    }

    [Fact]
    public async Task RegisterAsync_ShouldSetCreatedAtUtcTimestamp()
    {
        // Arrange
        var request = new RegisterRequest("+27821234567", "Password123!", "EN");
        var beforeCall = DateTime.UtcNow;

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        await service.RegisterAsync(request);

        var afterCall = DateTime.UtcNow;

        // Assert
        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u =>
                u.CreatedAt >= beforeCall &&
                u.CreatedAt <= afterCall),
            It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ShouldSucceed_WhenCredentialsAreValid()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "+27821234567",
            PhoneNumber = "+27821234567",
            PreferredLanguage = "EN",
            FicaVerified = true
        };

        _userManagerMock
            .Setup(x => x.FindByNameAsync(request.PhoneNumber))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

        _userManagerMock
            .Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        var (success, response, error) = await service.LoginAsync(request);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        response.Should().NotBeNull();
        response!.Token.Should().NotBeNullOrEmpty(); // Real JWT token generated
        response.RefreshToken.Should().NotBeNullOrEmpty(); // Real refresh token generated
        response.User.UserId.Should().Be(user.Id);
        response.User.PhoneNumber.Should().Be(user.PhoneNumber);
        response.User.PreferredLanguage.Should().Be(user.PreferredLanguage);
        response.User.FicaVerified.Should().BeTrue();
        response.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LoginAsync_ShouldStoreRefreshTokenInCache()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "+27821234567",
            PhoneNumber = "+27821234567",
            PreferredLanguage = "EN",
            FicaVerified = false
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.PhoneNumber)).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        await service.LoginAsync(request);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(
            $"refresh_token:{user.Id}",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromDays(30)),
            default),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldUpdateLastLoginTimestamp()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "+27821234567",
            PhoneNumber = "+27821234567"
        };

        var beforeLogin = DateTime.UtcNow;

        _userManagerMock.Setup(x => x.FindByNameAsync(request.PhoneNumber)).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        var service = CreateService();

        // Act
        await service.LoginAsync(request);

        var afterLogin = DateTime.UtcNow;

        // Assert
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeOnOrAfter(beforeLogin);
        user.LastLoginAt.Should().BeOnOrBefore(afterLogin);

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "Password123!");

        _userManagerMock
            .Setup(x => x.FindByNameAsync(request.PhoneNumber))
            .ReturnsAsync((ApplicationUser?)null);

        var service = CreateService();

        // Act
        var (success, response, error) = await service.LoginAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Invalid phone number or password");
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenPasswordIsInvalid()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "WrongPassword!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "+27821234567",
            PhoneNumber = "+27821234567"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.PhoneNumber)).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

        var service = CreateService();

        // Act
        var (success, response, error) = await service.LoginAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Invalid phone number or password");
    }

    [Fact]
    public async Task LoginAsync_ShouldFail_WhenAccountIsLockedOut()
    {
        // Arrange
        var request = new LoginRequest("+27821234567", "Password123!");
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "+27821234567",
            PhoneNumber = "+27821234567"
        };

        _userManagerMock.Setup(x => x.FindByNameAsync(request.PhoneNumber)).ReturnsAsync(user);
        _signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, request.Password, true))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

        var service = CreateService();

        // Act
        var (success, response, error) = await service.LoginAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Account is locked. Please try again later.");
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_ShouldSucceed_WhenTokensAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "+27821234567",
            PhoneNumber = "+27821234567",
            PreferredLanguage = "ZU",
            FicaVerified = true
        };

        var service = CreateService();
        
        // Generate a real access token to test with
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
        configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");
        var jwtService = new JwtTokenService(configMock.Object);
        
        var validAccessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = "valid_refresh_token";
        
        var request = new RefreshTokenRequest(validAccessToken, refreshToken);

        _cacheMock.Setup(x => x.GetAsync($"refresh_token:{userId}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(refreshToken));
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var (success, response, error) = await service.RefreshTokenAsync(request);

        // Assert
        success.Should().BeTrue();
        error.Should().BeNull();
        response.Should().NotBeNull();
        response!.Token.Should().NotBeNullOrEmpty(); // New token generated
        response.RefreshToken.Should().NotBeNullOrEmpty(); // New refresh token generated
        response.User.UserId.Should().Be(userId);
        response.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(60), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldStoreNewRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, PhoneNumber = "+27821234567" };

        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
        configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");
        var jwtService = new JwtTokenService(configMock.Object);
        var validAccessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = "valid_refresh_token";
        
        var request = new RefreshTokenRequest(validAccessToken, refreshToken);
        var service = CreateService();

        _cacheMock.Setup(x => x.GetAsync($"refresh_token:{userId}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(refreshToken));
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        await service.RefreshTokenAsync(request);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(
            $"refresh_token:{userId}",
            It.IsAny<byte[]>(),
            It.Is<DistributedCacheEntryOptions>(o =>
                o.AbsoluteExpirationRelativeToNow == TimeSpan.FromDays(30)),
            default),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldFail_WhenAccessTokenIsInvalid()
    {
        // Arrange
        var request = new RefreshTokenRequest("invalid_token", "refresh_token");
        var service = CreateService();

        // Act
        var (success, response, error) = await service.RefreshTokenAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldFail_WhenRefreshTokenDoesNotMatch()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, PhoneNumber = "+27821234567" };
        
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
        configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");
        var jwtService = new JwtTokenService(configMock.Object);
        var validAccessToken = jwtService.GenerateAccessToken(user);
        
        var request = new RefreshTokenRequest(validAccessToken, "wrong_refresh_token");
        var service = CreateService();

        _cacheMock.Setup(x => x.GetAsync($"refresh_token:{userId}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes("stored_refresh_token"));

        // Act
        var (success, response, error) = await service.RefreshTokenAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldFail_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, PhoneNumber = "+27821234567" };
        
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
        configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");
        var jwtService = new JwtTokenService(configMock.Object);
        var validAccessToken = jwtService.GenerateAccessToken(user);
        
        var request = new RefreshTokenRequest(validAccessToken, "valid_refresh_token");
        var service = CreateService();

        _cacheMock.Setup(x => x.GetAsync($"refresh_token:{userId}", default))
            .ReturnsAsync(Encoding.UTF8.GetBytes("valid_refresh_token"));
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync((ApplicationUser?)null);

        // Act
        var (success, response, error) = await service.RefreshTokenAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("User not found");
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldFail_WhenRefreshTokenNotFoundInCache()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new ApplicationUser { Id = userId, PhoneNumber = "+27821234567" };
        
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        configMock.Setup(x => x["JwtSettings:SecretKey"]).Returns("this-is-a-very-secret-key-that-is-at-least-32-characters-long");
        configMock.Setup(x => x["JwtSettings:Issuer"]).Returns("TestIssuer");
        configMock.Setup(x => x["JwtSettings:Audience"]).Returns("TestAudience");
        configMock.Setup(x => x["JwtSettings:ExpiryMinutes"]).Returns("60");
        var jwtService = new JwtTokenService(configMock.Object);
        var validAccessToken = jwtService.GenerateAccessToken(user);
        
        var request = new RefreshTokenRequest(validAccessToken, "valid_refresh_token");
        var service = CreateService();

        _cacheMock.Setup(x => x.GetAsync($"refresh_token:{userId}", default))
            .ReturnsAsync((byte[]?)null);

        // Act
        var (success, response, error) = await service.RefreshTokenAsync(request);

        // Assert
        success.Should().BeFalse();
        response.Should().BeNull();
        error.Should().Be("Invalid refresh token");
    }

    #endregion
}
