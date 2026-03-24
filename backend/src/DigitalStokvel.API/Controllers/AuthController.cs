using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalStokvel.Core.DTOs;
using DigitalStokvel.Services;
using DigitalStokvel.API.DTOs;

namespace DigitalStokvel.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthenticationService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with phone number and password
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, userId, error) = await _authService.RegisterAsync(request);

        if (!success)
        {
            _logger.LogWarning("Registration failed for phone {PhoneNumber}: {Error}", 
                request.PhoneNumber, error);
            
            return BadRequest(new ErrorResponse(
                Success: false,
                Message: "We couldn't create your account this time. Let's try again!",
                Timestamp: DateTime.UtcNow,
                ErrorCode: "REGISTRATION_FAILED",
                Errors: new Dictionary<string, string[]> { { "registration", new[] { error ?? "Unknown error" } } }));
        }

        _logger.LogInformation("User registered successfully: {UserId}", userId);

        return Ok(new ApiResponse<RegisterResponse>(
            Success: true,
            Message: "Welcome to Digital Stokvel! Your account is ready.",
            Timestamp: DateTime.UtcNow,
            Data: new RegisterResponse(
                UserId: userId!,
                Message: "Account created successfully")));
    }

    /// <summary>
    /// Login with phone number and password, returns JWT tokens
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, response, error) = await _authService.LoginAsync(request);

        if (!success)
        {
            _logger.LogWarning("Login failed for phone {PhoneNumber}: {Error}", 
                request.PhoneNumber, error);
            
            return Unauthorized(new ErrorResponse(
                Success: false,
                Message: "We couldn't log you in this time. Please check your details and try again.",
                Timestamp: DateTime.UtcNow,
                ErrorCode: "LOGIN_FAILED",
                Errors: new Dictionary<string, string[]> { { "authentication", new[] { error ?? "Invalid credentials" } } }));
        }

        _logger.LogInformation("User logged in successfully: {UserId}", response!.User.UserId);

        return Ok(new ApiResponse<LoginResponse>(
            Success: true,
            Message: "Welcome back! You're now signed in.",
            Timestamp: DateTime.UtcNow,
            Data: response));
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var (success, response, error) = await _authService.RefreshTokenAsync(request);

        if (!success)
        {
            _logger.LogWarning("Token refresh failed: {Error}", error);
            
            return Unauthorized(new ErrorResponse(
                Success: false,
                Message: "Your session has expired. Please log in again.",
                Timestamp: DateTime.UtcNow,
                ErrorCode: "TOKEN_REFRESH_FAILED",
                Errors: new Dictionary<string, string[]> { { "token", new[] { error ?? "Invalid token" } } }));
        }

        return Ok(new ApiResponse<LoginResponse>(
            Success: true,
            Message: "Your session has been refreshed.",
            Timestamp: DateTime.UtcNow,
            Data: response));
    }

    /// <summary>
    /// Test endpoint to verify authentication is working (requires valid JWT)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var phoneNumber = User.FindFirst(System.Security.Claims.ClaimTypes.MobilePhone)?.Value;

        return Ok(new ApiResponse<object>(
            Success: true,
            Message: "Authentication verified",
            Timestamp: DateTime.UtcNow,
            Data: new
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            }));
    }
}
