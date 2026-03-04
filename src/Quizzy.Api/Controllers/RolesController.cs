using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager)
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
}
