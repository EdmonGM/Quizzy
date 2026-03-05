using Microsoft.AspNetCore.Identity;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

public static class UserSeeder
{
    /// <summary>
    /// System user ID for orphaned content when users delete their accounts
    /// </summary>
    public const string DeletedUserId = "deleted-user-0000-0000-000000000000";
    private const string DeletedUserName = "Deleted_Account";
    private const string DeletedUserEmail = "deleted@system.local";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserSeeder");

        // Create or update the DeletedUser system account
        var deletedUser = await userManager.FindByIdAsync(DeletedUserId);

        if (deletedUser == null)
        {
            deletedUser = new ApplicationUser
            {
                Id = DeletedUserId,
                UserName = DeletedUserName,
                Email = DeletedUserEmail,
                EmailConfirmed = true,
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = false,
                AccessFailedCount = 0
            };

            // Create with a random password that no one can know
            var result = await userManager.CreateAsync(deletedUser, Guid.NewGuid().ToString("N"));

            if (result.Succeeded)
            {
                logger.LogInformation("Created system DeletedUser account with ID: {UserId}", DeletedUserId);
            }
            else
            {
                logger.LogError("Failed to create DeletedUser account: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            // Ensure the system user exists and is properly configured
            if (deletedUser.UserName != DeletedUserName || deletedUser.Email != DeletedUserEmail)
            {
                deletedUser.UserName = DeletedUserName;
                deletedUser.Email = DeletedUserEmail;
                await userManager.UpdateAsync(deletedUser);
                logger.LogInformation("Updated DeletedUser account");
            }
        }
    }

    /// <summary>
    /// Get the DeletedUser system account ID
    /// </summary>
    public static string GetDeletedUserId() => DeletedUserId;
}
