using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;
using Quizzy.Api.Services;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Handles authentication operations including registration, login, token refresh, and admin creation.
/// </summary>
/// <remarks>
/// Login, Register, and Refresh endpoints do not require authentication. CreateAdmin requires Admin role.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    ILogger<AuthController> logger,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    private const string RoleStudent = "Student";
    private const string RoleTeacher = "Teacher";
    private const string RoleAdmin = "Admin";

    /// <summary>
    /// Register a new user account with Student or Teacher role.
    /// </summary>
    /// <param name="dto">The registration details including username, email, password, and role.</param>
    /// <returns>The created user profile.</returns>
    /// <response code="200">Returns the created user profile.</response>
    /// <response code="400">If registration fails or role is invalid.</response>
    /// <response code="429">If too many registration attempts from the same IP.</response>
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role is not RoleStudent and not RoleTeacher)
        {
            return BadRequest(new { message = "Invalid role. Must be 'Student' or 'Teacher'." });
        }

        var user = dto.ToApplicationUser();

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Registration failed", errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, dto.Role);

        var profile = new UserProfile
        {
            UserId = user.Id,
            User = user,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.UserProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        logger.LogInformation("User {Username} registered successfully as {Role}", user.UserName, dto.Role);

        return Ok(profile.ToUserProfileDto(user.UserName!, user.Email!));
    }

    /// <summary>
    /// Create an admin user (Admin only)
    /// </summary>
    [HttpPost("create-admin")]
    [Authorize(Roles = RoleAdmin)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
    {
        var user = dto.ToApplicationUser();

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { message = "Admin creation failed", errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, RoleAdmin);

        var profile = new UserProfile
        {
            UserId = user.Id,
            User = user,
            FirstName = dto.Username,
            LastName = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await context.UserProfiles.AddAsync(profile);
        await context.SaveChangesAsync();

        logger.LogInformation("Admin user {Username} created successfully by {CurrentUser}", user.UserName, User.Identity?.Name);

        var userResponseDto = user.ToUserResponseDto();

        return Ok(new { message = "Admin user created successfully", user = userResponseDto });
    }

    /// <summary>
    /// Login with username and password and receive JWT token
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.Username);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var result = await userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
        {
            return Unauthorized(new { message = "Invalid username or password" });
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return Ok(new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Username = user.UserName!,
            Email = user.Email!,
            Roles = roles
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var principal = jwtTokenService.GetPrincipalFromExpiredToken(dto.Token);

        if (principal == null)
        {
            return BadRequest(new { message = "Invalid access token" });
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
        {
            return BadRequest(new { message = "Invalid token claims" });
        }

        var refreshTokenPrincipal = jwtTokenService.GetPrincipalFromRefreshToken(dto.RefreshToken);

        if (refreshTokenPrincipal == null)
        {
            return BadRequest(new { message = "Invalid or expired refresh token" });
        }

        var refreshTokenUserId = refreshTokenPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != refreshTokenUserId)
        {
            return BadRequest(new { message = "Token mismatch" });
        }

        var user = await userManager.GetUserAsync(principal);

        if (user == null)
        {
            return Unauthorized(new { message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);
        var newToken = jwtTokenService.GenerateToken(user, roles);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken(user);

        logger.LogInformation("User {Username} refreshed token successfully", username);

        return Ok(new RefreshResponseDto
        {
            Token = newToken,
            RefreshToken = newRefreshToken
        });
    }
}
