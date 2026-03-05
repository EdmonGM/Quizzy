using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Data;
using Quizzy.Api.Models;

namespace Quizzy.Api.Services;

public interface IAccountDeletionService
{
    /// <summary>
    /// Deletes a user account permanently, transferring their quizzes to the system DeletedUser account
    /// </summary>
    /// <param name="userId">The ID of the user to delete</param>
    /// <returns>Result of the deletion operation</returns>
    Task<IdentityResult> DeleteUserAsync(string userId);
}

public class AccountDeletionService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    ILogger<AccountDeletionService> logger)
    : IAccountDeletionService
{
    public async Task<IdentityResult> DeleteUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "UserNotFound",
                Description = "User not found"
            });
        }

        // Prevent deleting the system DeletedUser account
        if (userId == UserSeeder.DeletedUserId)
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "CannotDeleteSystemUser",
                Description = "Cannot delete system account"
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            // Step 1: Transfer quiz ownership to DeletedUser
            var transferredQuizCount = await dbContext.Quizzes
                .Where(q => q.TeacherId == userId)
                .ExecuteUpdateAsync(q => q.SetProperty(x => x.TeacherId, UserSeeder.DeletedUserId));

            if (transferredQuizCount > 0)
            {
                logger.LogInformation("Transferred {Count} quizzes from user {UserId} to DeletedUser", 
                    transferredQuizCount, userId);
            }

            // Step 2: Delete user profile (if exists)
            var profile = await dbContext.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile != null)
            {
                dbContext.UserProfiles.Remove(profile);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Deleted UserProfile for user {UserId}", userId);
            }

            // Step 3: Delete the user
            var result = await userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                await transaction.CommitAsync();
                logger.LogInformation("User {UserId} deleted successfully. {Count} quizzes transferred.", 
                    userId, transferredQuizCount);
            }
            else
            {
                await transaction.RollbackAsync();
                logger.LogError("Failed to delete user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error deleting user {UserId}", userId);
            return IdentityResult.Failed(new IdentityError
            {
                Code = "DeletionError",
                Description = $"An error occurred while deleting the user: {ex.Message}"
            });
        }
    }
}
