using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Manages role assignment operations including listing roles, viewing users in a role, and adding/removing users from roles.
/// </summary>
/// <remarks>
/// This controller requires authentication for all endpoints. Role modification endpoints require Admin role.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RolesController(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    ILogger<RolesController> logger)
    : ControllerBase
{
    private const string RoleAdmin = "Admin";
    private const string RoleTeacher = "Teacher";
    private const string RoleStudent = "Student";
    private static readonly string[] ValidRoles = [RoleAdmin, RoleTeacher, RoleStudent];

    /// <summary>
    /// Retrieves all available roles in the system.
    /// </summary>
    /// <returns>List of role IDs and names.</returns>
    /// <response code="200">Returns the list of roles.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet]
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await roleManager.Roles.ToListAsync();

        return Ok(roles.Select(r => new { roleId = r.Id, roleName = r.Name }));
    }

    /// <summary>
    /// Retrieves all users assigned to a specific role.
    /// </summary>
    /// <param name="name">The name of the role (Admin, Teacher, or Student).</param>
    /// <returns>List of users in the specified role.</returns>
    /// <response code="200">Returns the list of users in the role.</response>
    /// <response code="400">If the role name is invalid.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="404">If the role is not found.</response>
    [HttpGet("{name}/users")]
    [Authorize(Roles = RoleAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsersInRole(string name)
    {
        if (!ValidRoles.Contains(name))
        {
            return BadRequest(new { message = $"Invalid role name. Valid roles are: {string.Join(", ", ValidRoles)}" });
        }

        var users = await userManager.GetUsersInRoleAsync(name);

        if (!users.Any())
        {
            var roleExists = await roleManager.RoleExistsAsync(name);
            if (!roleExists)
            {
                return NotFound(new { message = "Role not found" });
            }
        }

        return Ok(users.Select(u => u.ToUserResponseDto([name])));
    }

    /// <summary>
    /// Adds a user to a specific role.
    /// </summary>
    /// <param name="id">The GUID of the user to add to the role.</param>
    /// <param name="role">The role name to assign (Admin, Teacher, or Student).</param>
    /// <response code="200">User was successfully added to the role.</response>
    /// <response code="400">If the user ID format is invalid, the role is invalid, or the operation failed.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="404">If the user is not found.</response>
    /// <response code="409">If the user is already in the specified role.</response>
    [HttpPost("{id}/roles/{role}")]
    [Authorize(Roles = RoleAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddToRole(string id, string role)
    {
        if (!Guid.TryParse(id, out _))
        {
            return BadRequest(new { message = "Invalid user ID format" });
        }

        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!ValidRoles.Contains(role))
        {
            return BadRequest(new { message = $"Invalid role. Valid roles are: {string.Join(", ", ValidRoles)}" });
        }

        var result = await userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == "DuplicateRoleName" || e.Description.Contains("already")))
            {
                return Conflict(new { message = $"User is already in role {role}" });
            }

            logger.LogWarning("Failed to add user {UserId} to role {Role}: {Errors}",
                user.Id, role, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "Failed to add user to role" });
        }

        logger.LogInformation("User {UserId} added to role {Role}", user.Id, role);

        return Ok(new { message = $"User added to role {role}" });
    }

    /// <summary>
    /// Removes a user from a specific role.
    /// </summary>
    /// <param name="id">The GUID of the user to remove from the role.</param>
    /// <param name="role">The role name to remove (Admin, Teacher, or Student).</param>
    /// <response code="200">User was successfully removed from the role.</response>
    /// <response code="400">If the user ID format is invalid, the role is invalid, or the operation failed.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="403">If the current user does not have Admin role.</response>
    /// <response code="404">If the user is not found or is not in the specified role.</response>
    [HttpDelete("{id}/roles/{role}")]
    [Authorize(Roles = RoleAdmin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFromRole(string id, string role)
    {
        if (!Guid.TryParse(id, out _))
        {
            return BadRequest(new { message = "Invalid user ID format" });
        }

        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!ValidRoles.Contains(role))
        {
            return BadRequest(new { message = $"Invalid role. Valid roles are: {string.Join(", ", ValidRoles)}" });
        }

        var result = await userManager.RemoveFromRoleAsync(user, role);

        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Description.Contains("not") || e.Description.Contains("not found")))
            {
                return NotFound(new { message = $"User is not in role {role}" });
            }

            logger.LogWarning("Failed to remove user {UserId} from role {Role}: {Errors}",
                user.Id, role, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "Failed to remove user from role" });
        }

        logger.LogInformation("User {UserId} removed from role {Role}", user.Id, role);

        return Ok(new { message = $"User removed from role {role}" });
    }
}
