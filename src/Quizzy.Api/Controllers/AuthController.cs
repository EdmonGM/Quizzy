using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;
using Quizzy.Api.Services;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(UserManager<ApplicationUser> userManager, ILogger<AuthController> logger, IJwtTokenService jwtTokenService):ControllerBase
{
    /// <summary>
    /// Register a new user
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var user = model.ToApplicationUser();

        var result = await userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        await userManager.AddToRoleAsync(user, "Student");

        logger.LogInformation("User {Username} registered successfully", user.UserName);

        return Ok(user.ToUserResponseDto());
    }

    /// <summary>
    /// Login with username and password and receive JWT token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var user = await userManager.FindByNameAsync(model.Username);

        if (user == null)
        {
            return Unauthorized(new { Message = "Invalid username or password" });
        }

        var result = await userManager.CheckPasswordAsync(user, model.Password);

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
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto model)
    {
        var principal = jwtTokenService.GetPrincipalFromExpiredToken(model.Token);

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

        var refreshTokenPrincipal = jwtTokenService.GetPrincipalFromRefreshToken(model.RefreshToken);

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