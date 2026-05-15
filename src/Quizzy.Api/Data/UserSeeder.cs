using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Default admin credentials for development/initial setup
    /// </summary>
    private const string DefaultAdminUsername = "admin";
    private const string DefaultAdminEmail = "admin@gmail.com";
    private const string DefaultAdminPassword = "123123123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
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

        // Create default admin if no admins exist
        await CreateDefaultAdminIfNeeded(userManager, roleManager, logger);
    }

    /// <summary>
    /// Creates a default admin user if no admin users currently exist in the system.
    /// This is useful for initial setup and development scenarios.
    /// </summary>
    private static async Task CreateDefaultAdminIfNeeded(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ILogger logger)
    {
        // Check if any admins already exist
        var adminRole = await roleManager.FindByNameAsync("Admin");
        if (adminRole == null)
        {
            logger.LogWarning("Admin role not found. Skipping default admin creation.");
            return;
        }

        var adminsExist = await userManager.GetUsersInRoleAsync("Admin");
        if (adminsExist.Any())
        {
            logger.LogInformation("Admin user(s) already exist. Skipping default admin creation.");
            return;
        }

        // Create default admin account
        var adminUser = new ApplicationUser
        {
            UserName = DefaultAdminUsername,
            Email = DefaultAdminEmail,
            EmailConfirmed = true,
            PhoneNumberConfirmed = false,
            TwoFactorEnabled = false,
            LockoutEnabled = false,
            AccessFailedCount = 0
        };

        var result = await userManager.CreateAsync(adminUser, DefaultAdminPassword);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to create default admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        // Add Admin role to the new user
        var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");

        if (!roleResult.Succeeded)
        {
            logger.LogError("Failed to assign Admin role to default admin user: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        logger.LogInformation(
            "Created default admin user with username '{Username}' and email '{Email}'. " +
            "Please change the password immediately in a production environment.",
            DefaultAdminUsername,
            DefaultAdminEmail);
    }

    /// <summary>
    /// Get the DeletedUser system account ID
    /// </summary>
    public static string GetDeletedUserId() => DeletedUserId;
}