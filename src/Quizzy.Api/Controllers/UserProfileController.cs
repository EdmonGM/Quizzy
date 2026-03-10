using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Quizzy.Api.Data;
using Quizzy.Api.Dtos;
using Quizzy.Api.Mappers;
using Quizzy.Api.Models;

namespace Quizzy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext context,
    ILogger<UserProfileController> logger)
    : ControllerBase
{
    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        var profile = await context.UserProfiles.FindAsync(userId);

        if (profile == null)
        {
            return NotFound(new { Message = "Profile not found" });
        }

        return Ok(profile.ToUserProfileDto(user.UserName!, user.Email!));
    }

    /// <summary>
    /// Get user profile by user ID
    /// </summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserProfile(string userId)
    {
        var profile = await context.UserProfiles.FindAsync(userId);

        if (profile == null)
        {
            return NotFound(new { Message = "Profile not found" });
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { Message = "User not found" });
        }

        return Ok(profile.ToUserProfileDto(user.UserName!, user.Email!));
    }

    /// <summary>
    /// Create or update current user's profile
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrUpdateProfile([FromBody] UpdateUserProfileDto model)
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var profile = await context.UserProfiles.FindAsync(userId);

        if (profile == null)
        {
            var user = await userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            profile = new UserProfile
            {
                UserId = userId,
                FirstName = model.FirstName ?? string.Empty,
                LastName = model.LastName ?? string.Empty,
                Bio = model.Bio,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.UserProfiles.Add(profile);
            logger.LogInformation("Profile created for user {UserId}", userId);
        }
        else
        {
            if (!string.IsNullOrEmpty(model.FirstName))
            {
                profile.FirstName = model.FirstName;
            }

            if (!string.IsNullOrEmpty(model.LastName))
            {
                profile.LastName = model.LastName;
            }

            profile.Bio = model.Bio;
            profile.UpdatedAt = DateTime.UtcNow;

            logger.LogInformation("Profile updated for user {UserId}", userId);
        }

        await context.SaveChangesAsync();

        var applicationUser = await userManager.FindByIdAsync(userId);

        return Ok(profile.ToUserProfileDto(applicationUser!.UserName!, applicationUser.Email!));
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto model)
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var profile = await context.UserProfiles.FindAsync(userId);

        if (profile == null)
        {
            return NotFound(new { Message = "Profile not found. Please create a profile first." });
        }

        if (!string.IsNullOrEmpty(model.FirstName))
        {
            profile.FirstName = model.FirstName;
        }

        if (!string.IsNullOrEmpty(model.LastName))
        {
            profile.LastName = model.LastName;
        }

        profile.Bio = model.Bio;
        profile.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        var user = await userManager.FindByIdAsync(userId);

        logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(profile.ToUserProfileDto(user!.UserName!, user.Email!));
    }

    /// <summary>
    /// Update current user's profile image URL
    /// </summary>
    [HttpPut("image")]
    public async Task<IActionResult> UpdateProfileImage([FromBody] UpdateProfileImageDto model)
    {
        var userId = userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { Message = "User not authenticated" });
        }

        var profile = await context.UserProfiles.FindAsync(userId);

        if (profile == null)
        {
            return NotFound(new { Message = "Profile not found" });
        }

        profile.ProfileImageUrl = model.ProfileImageUrl;
        profile.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();

        logger.LogInformation("Profile image updated for user {UserId}", userId);

        var user = await userManager.FindByIdAsync(userId);

        return Ok(profile.ToUserProfileDto(user!.UserName!, user.Email!));
    }
}
