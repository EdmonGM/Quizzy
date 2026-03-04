using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<AccountController> logger)
    : ControllerBase
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
    /// Login with username and password
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

        logger.LogInformation("User {Username} logged in successfully", user.UserName);

        return Ok(user.ToUserResponseDto(roles));
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(user.ToUserResponseDto(roles));
    }

    /// <summary>
    /// Get user by username
    /// </summary>
    [HttpGet("username/{username}")]
    public async Task<IActionResult> GetUserByUsername(string username)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(user.ToUserResponseDto(roles));
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = userManager.Users.ToList();
        var result = new List<UserResponseDto>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            result.Add(user.ToUserResponseDto(roles));
        }

        return Ok(result);
    }

    /// <summary>
    /// Update user email
    /// </summary>
    [HttpPut("{id}/email")]
    public async Task<IActionResult> UpdateEmail(string id, [FromBody] UpdateEmailDto model)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, model.Email);
        var result = await userManager.ChangeEmailAsync(user, model.Email, token);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} email updated to {Email}", user.Id, model.Email);

        return Ok(new { Message = "Email updated successfully" });
    }

    /// <summary>
    /// Update user password
    /// </summary>
    [HttpPut("{id}/password")]
    public async Task<IActionResult> UpdatePassword(string id, [FromBody] UpdatePasswordDto model)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} password updated successfully", user.Id);

        return Ok(new { Message = "Password updated successfully" });
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var result = await userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} deleted successfully", user.Id);

        return Ok(new { Message = "User deleted successfully" });
    }

    /// <summary>
    /// Add user to role
    /// </summary>
    [HttpPost("{id}/roles/{role}")]
    public async Task<IActionResult> AddToRole(string id, string role)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new { Message = $"Invalid role. Valid roles are: {string.Join(", ", await roleManager.Roles.ToListAsync())}" });
        }

        var result = await userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} added to role {Role}", user.Id, role);

        return Ok(new { Message = $"User added to role {role}" });
    }

    /// <summary>
    /// Remove user from role
    /// </summary>
    [HttpDelete("{id}/roles/{role}")]
    public async Task<IActionResult> RemoveFromRole(string id, string role)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        if (!await roleManager.RoleExistsAsync(role))
        {
            return BadRequest(new { Message = $"Invalid role. Valid roles are: {string.Join(", ", await roleManager.Roles.ToListAsync())}" });
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} removed from role {Role}", user.Id, role);

        return Ok(new { Message = $"User removed from role {role}" });
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    [HttpGet("{id}/roles")]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new { UserId = user.Id, Roles = roles });
    }
}
