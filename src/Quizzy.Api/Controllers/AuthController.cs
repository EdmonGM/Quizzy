using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Constants;
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
    /// <summary>
    /// Register a new user account with Student or Teacher role.
    /// </summary>
    /// <param name="dto">The registration details including username, email, password, and role.</param>
    /// <returns>The created user profile.</returns>
    /// <response code="201">Returns the created user profile.</response>
    /// <response code="400">If registration fails or role is invalid.</response>
    /// <response code="429">If too many registration attempts from the same IP.</response>
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role is not AppRoles.Student and not AppRoles.Teacher)
        {
            return BadRequest("Invalid role. Must be 'Student' or 'Teacher'.");
        }

        var user = dto.ToApplicationUser();

        var (createdUser, profile, error) = await CreateUserAndProfileAsync(user, dto.Password, dto.Role, dto.FirstName, dto.LastName);
        if (error != null) return error;

        logger.LogInformation("User {Username} registered successfully as {Role}", createdUser!.UserName, dto.Role);

        return Created(string.Empty, profile!.ToUserProfileDto(createdUser.UserName ?? string.Empty, createdUser.Email ?? string.Empty));
    }

    /// <summary>
    /// Create an admin user (Admin only)
    /// </summary>
    [HttpPost("create-admin")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
    {
        var user = dto.ToApplicationUser();

        var (createdUser, _, error) = await CreateUserAndProfileAsync(user, dto.Password, AppRoles.Admin, dto.FirstName, dto.LastName);
        if (error != null) return error;

        logger.LogInformation("Admin user {Username} created successfully by {CurrentUser}", createdUser!.UserName, User.Identity?.Name);

        var userResponseDto = createdUser.ToUserResponseDto();

        return Ok(userResponseDto);
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
            return Unauthorized("Invalid username or password");
        }

        var result = await userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
        {
            return Unauthorized("Invalid username or password");
        }

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles);
        var refreshToken = jwtTokenService.GenerateRefreshToken(user);

        logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return Ok(new LoginResponseDto
        {
            Token = token,
            RefreshToken = refreshToken,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting("refresh")]
    [ProducesResponseType(typeof(RefreshResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
    {
        var principal = jwtTokenService.GetPrincipalFromExpiredToken(dto.Token);

        if (principal == null)
        {
            return BadRequest("Invalid access token");
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
        {
            return BadRequest("Invalid token claims");
        }

        var refreshTokenPrincipal = jwtTokenService.GetPrincipalFromRefreshToken(dto.RefreshToken);

        if (refreshTokenPrincipal == null)
        {
            return BadRequest("Invalid or expired refresh token");
        }

        var refreshTokenUserId = refreshTokenPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != refreshTokenUserId)
        {
            return BadRequest("Token mismatch");
        }

        var user = await userManager.GetUserAsync(principal);

        if (user == null)
        {
            return Unauthorized("User not found");
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

    private async Task<(ApplicationUser? User, UserProfile? Profile, IActionResult? Error)> CreateUserAndProfileAsync(
        ApplicationUser user, string password, string role, string firstName, string lastName)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return (null, null, BadRequest("User creation failed"));
            }

            await userManager.AddToRoleAsync(user, role);

            var profile = new UserProfile
            {
                UserId = user.Id,
                User = user,
                FirstName = firstName,
                LastName = lastName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserProfiles.Add(profile);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (user, profile, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error creating user {Username}", user.UserName);
            return (null, null, StatusCode(500, "An error occurred during user creation"));
        }
    }
}
