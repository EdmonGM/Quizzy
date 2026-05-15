using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;
using Quizzy.Api.Services;

namespace Quizzy.Api.Controllers;

/// <summary>
/// Manages user account operations including, get users (all or by id / username), get provided user roles, update (email, password) and delete.
/// </summary>
/// <remarks>
/// This controller require authentication for all endpoints.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController(
    UserManager<ApplicationUser> userManager,
    IAccountDeletionService accountDeletionService,
    ILogger<AccountsController> logger)
    : ControllerBase
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The GUID of the user to retrieve.</param>
    /// <returns>The user details if found.</returns>
    /// <response code="200">Returns the requested user.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponseDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Retrieves a user by their username.
    /// </summary>
    /// <param name="username">The username of the user to retrieve.</param>
    /// <returns>The user details if found.</returns>
    /// <response code="200">Returns the requested user.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("username/{username}")]
    [ProducesResponseType(typeof(UserResponseDto),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Get all users.
    /// </summary>
    /// <remarks>This endpoint requires Admin role.</remarks>
    /// <returns>List of users details if found.</returns>
    /// <response code="200">Returns the list of users.</response>
    /// <response code="401">If the request is not authenticated.</response>
    [HttpGet]
    [Authorize (Roles = "Admin")]
    [ProducesResponseType(typeof(List<UserResponseDto>),StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    /// Update current user email.
    /// </summary>
    /// <remarks>This endpoint is only accessible by currently logged-in user.</remarks>
    /// <response code="200"></response>
    /// <response code="400">If changing email failed.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpPut("me/email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmail([FromBody] string email)
    {
                
        var currentUserId = userManager.GetUserId(User);
        if (currentUserId == null)
        {
            return Unauthorized();
        }
        var currentUser = await userManager.FindByIdAsync(currentUserId);
        if (currentUser == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(currentUser, email);
        var result = await userManager.ChangeEmailAsync(currentUser, email, token);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} email updated to {Email}", currentUser.Id, email);

        return Ok(new { Message = "Email updated successfully" });
    }

    /// <summary>
    /// Update current user password
    /// </summary>
    /// <remarks>This endpoint is only accessible by currently logged-in user.</remarks>
    /// <response code="200"></response>
    /// <response code="400">If changing password failed.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordDto dto)
    {
        var currentUserId = userManager.GetUserId(User);
        if (currentUserId == null)
        {
            return Unauthorized();
        }
        var currentUser = await userManager.FindByIdAsync(currentUserId);
        if (currentUser == null)
        {
            return NotFound(new { Message = "User not found" });
        }
        
        var result = await userManager.ChangePasswordAsync(currentUser, dto.CurrentPassword, dto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequest(new { Errors = result.Errors.Select(e => e.Description) });
        }

        logger.LogInformation("User {UserId} password updated successfully", currentUser.Id);

        return Ok(new { Message = "Password updated successfully" });
    }

    /// <summary>
    /// Delete current user account permanently. Quizzes created by the user will be transferred to a system account.
    /// </summary>
    /// <remarks>This endpoint is only accessible by currently logged-in user.</remarks>
    /// <response code="200"></response>
    /// <response code="400">If deleting failed.</response>
    /// <response code="401">If the request is not authenticated.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser()
    {
        var currentUserId = userManager.GetUserId(User);
        if (currentUserId == null)
        {
            return Unauthorized();
        }
        var result = await accountDeletionService.DeleteUserAsync(currentUserId);

        if (!result.Succeeded)
        {
            var error = result.Errors.FirstOrDefault();
            return error?.Code switch
            {
                "UserNotFound" => NotFound(new { Message = error.Description }),
                "CannotDeleteSystemUser" => BadRequest(new { Message = error.Description }),
                _ => BadRequest(new { Errors = result.Errors.Select(e => e.Description) })
            };
        }

        logger.LogInformation("User {UserId} deleted successfully", currentUserId);

        return Ok(new { Message = "User deleted successfully" });
    }

    /// <summary>
    /// Retrieves a user roles by their unique identifier.
    /// </summary>
    /// <param name="id">The GUID of the user to retrieve their roles.</param>
    /// <returns>The user roles if found.</returns>
    /// <response code="200">Returns the requested user roles.</response>
    /// <response code="404">If the user is not found.</response>
    [HttpGet("{id}/roles")]
    [ProducesResponseType(typeof(UserRolesResponseDto),StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserRoles(string id)
    {
        var user = await userManager.FindByIdAsync(id);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new UserResponseDto { UserId = user.Id, Roles = roles });
    }
}
