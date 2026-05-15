using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<RolesController> logger)
    : ControllerBase
{
    /// <summary>
    /// Get all roles (Admin, Teacher, Student)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await roleManager.Roles.ToListAsync();

        return Ok(roles.Select(r => new { RoleId = r.Id, RoleName = r.Name }));
    }

    /// <summary>
    /// Get users in a role
    /// </summary>
    [HttpGet("{name}/users")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsersInRole(string name)
    {
        var role = await roleManager.FindByNameAsync(name);

        if (role == null)
        {
            return NotFound(new { Message = "Role not found" });
        }

        var users = await userManager.GetUsersInRoleAsync(role.Name!);

        return Ok(users.Select(u => u.ToUserResponseDto([role.Name!])));
    }
    
    /// <summary>
    /// Add user to role
    /// </summary>
    [HttpPost("{id}/roles/{role}")]
    [Authorize(Roles = "Admin")]
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
    [Authorize(Roles = "Admin")]
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
}
