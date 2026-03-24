using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using DigitalStokvel.Core.Entities;
using DigitalStokvel.Core.DTOs;

namespace DigitalStokvel.Services;

/// <summary>
/// Service for user authentication and registration
/// </summary>
public class AuthenticationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IDistributedCache _cache;

    public AuthenticationService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IDistributedCache cache)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _cache = cache;
    }

    /// <summary>
    /// Registers a new user with phone number authentication
    /// </summary>
    public async Task<(bool Success, string? UserId, string? Error)> RegisterAsync(
        RegisterRequest request)
    {
        // Validate phone number format (South African: starts with +27 or 0)
        if (!IsValidSouthAfricanPhoneNumber(request.PhoneNumber))
        {
            return (false, null, "Invalid South African phone number format");
        }

        var user = new ApplicationUser
        {
            UserName = request.PhoneNumber,
            PhoneNumber = request.PhoneNumber,
            PreferredLanguage = request.PreferredLanguage,
            FicaVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return (false, null, errors);
        }

        return (true, user.Id, null);
    }

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    public async Task<(bool Success, LoginResponse? Response, string? Error)> LoginAsync(
        LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.PhoneNumber);
        if (user == null)
        {
            return (false, null, "Invalid phone number or password");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
            {
                return (false, null, "Account is locked. Please try again later.");
            }
            return (false, null, "Invalid phone number or password");
        }

        // Generate tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store refresh token in Redis with 30-day expiration
        await StoreRefreshTokenAsync(user.Id, refreshToken, TimeSpan.FromDays(30));

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var response = new LoginResponse(
            Token: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60),
            User: new UserInfo(
                UserId: user.Id,
                PhoneNumber: user.PhoneNumber ?? "",
                PreferredLanguage: user.PreferredLanguage,
                FicaVerified: user.FicaVerified));

        return (true, response, null);
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token
    /// </summary>
    public async Task<(bool Success, LoginResponse? Response, string? Error)> RefreshTokenAsync(
        RefreshTokenRequest request)
    {
        var userId = _jwtTokenService.GetUserIdFromToken(request.Token);
        if (userId == null)
        {
            return (false, null, "Invalid token");
        }

        var storedRefreshToken = await GetRefreshTokenAsync(userId);
        if (storedRefreshToken != request.RefreshToken)
        {
            return (false, null, "Invalid refresh token");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, null, "User not found");
        }

        // Generate new tokens
        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        // Store new refresh token
        await StoreRefreshTokenAsync(user.Id, refreshToken, TimeSpan.FromDays(30));

        var response = new LoginResponse(
            Token: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddMinutes(60),
            User: new UserInfo(
                UserId: user.Id,
                PhoneNumber: user.PhoneNumber ?? "",
                PreferredLanguage: user.PreferredLanguage,
                FicaVerified: user.FicaVerified));

        return (true, response, null);
    }

    private async Task StoreRefreshTokenAsync(string userId, string refreshToken, TimeSpan expiration)
    {
        var key = $"refresh_token:{userId}";
        await _cache.SetStringAsync(key, refreshToken, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }

    private async Task<string?> GetRefreshTokenAsync(string userId)
    {
        var key = $"refresh_token:{userId}";
        return await _cache.GetStringAsync(key);
    }

    private static bool IsValidSouthAfricanPhoneNumber(string phoneNumber)
    {
        // Remove spaces and dashes
        phoneNumber = phoneNumber.Replace(" ", "").Replace("-", "");

        // Accept formats: +27XXXXXXXXX or 0XXXXXXXXXX
        return (phoneNumber.StartsWith("+27") && phoneNumber.Length == 12) ||
               (phoneNumber.StartsWith("0") && phoneNumber.Length == 10);
    }
}
