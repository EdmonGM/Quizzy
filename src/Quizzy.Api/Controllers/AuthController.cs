using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;
using Quizzy.Api.Services;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    ILogger<AuthController> logger,
    IJwtTokenService jwtTokenService) : ControllerBase
{
    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (dto.Role is not "Student" and not "Teacher")
        {
            return BadRequest(new { Message = "Invalid role. Must be 'Student' or 'Teacher'." });
        }

        var user = dto.ToApplicationUser();
        
        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateAdminDto dto)
    {
        var user = dto.ToApplicationUser();

        var result = await userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, "Admin");
        
        await context.SaveChangesAsync();

        logger.LogInformation("Admin user {Username} created successfully by {CurrentUser}", user.UserName, User.Identity?.Name);

        var userResponseDto = user.ToUserResponseDto();

        return Ok(new { Message = "Admin user created successfully", userResponseDto });
    }

    /// <summary>
    /// Login with username and password and receive JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.Username);

        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid username or password" });
        }

        var result = await userManager.CheckPasswordAsync(user, dto.Password);

        if (!result)
        {
            return Unauthorized(new { Message = "Invalid username or password" });
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
            return BadRequest(new { Message = "Invalid access token" });
        }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var username = principal.FindFirstValue(ClaimTypes.Name);

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(username))
        {
            return BadRequest(new { Message = "Invalid token claims" });
        }

        var refreshTokenPrincipal = jwtTokenService.GetPrincipalFromRefreshToken(dto.RefreshToken);

        if (refreshTokenPrincipal == null)
        {
            return BadRequest(new { Message = "Invalid or expired refresh token" });
        }

        var refreshTokenUserId = refreshTokenPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != refreshTokenUserId)
        {
            return BadRequest(new { Message = "Token mismatch" });
        }

        var user = await userManager.GetUserAsync(principal);

        if (user == null)
        {
            return Unauthorized(new { Message = "User not found" });
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