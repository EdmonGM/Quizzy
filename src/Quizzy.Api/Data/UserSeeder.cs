using Microsoft.AspNetCore.Identity;
using Quizzy.Api.Constants;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

public static class UserSeeder
{
    public const string DeletedUserId = "deleted-user-0000-0000-000000000000";
    private const string DeletedUserName = "Deleted_Account";
    private const string DeletedUserEmail = "deleted@system.local";

    private const string AdminUserId = "admin-user-0000-0000-000000000000";
    public const string TeacherUserId = "teacher-user-000-0000-000000000000";
    private const string StudentUserId = "student-user-000-0000-000000000000";

    private const string AdminUsername = "admin";
    private const string AdminEmail = "admin@gmail.com";
    private const string AdminPassword = "123123123";

    private const string TeacherUsername = "teacher";
    private const string TeacherEmail = "teacher@gmail.com";
    private const string TeacherPassword = "123123123";

    private const string StudentUsername = "student";
    private const string StudentEmail = "student@gmail.com";
    private const string StudentPassword = "123123123";

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("UserSeeder");

        await SeedSystemDeletedUserAsync(userManager, logger);
        await SeedRoleAccountAsync(userManager, dbContext, logger, AdminUserId, AdminUsername, AdminEmail, AdminPassword, AppRoles.Admin);
        await SeedRoleAccountAsync(userManager, dbContext, logger, TeacherUserId, TeacherUsername, TeacherEmail, TeacherPassword, AppRoles.Teacher);
        await SeedRoleAccountAsync(userManager, dbContext, logger, StudentUserId, StudentUsername, StudentEmail, StudentPassword, AppRoles.Student);
    }

    private static async Task SeedSystemDeletedUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger)
    {
        var existing = await userManager.FindByIdAsync(DeletedUserId);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            Id = DeletedUserId,
            UserName = DeletedUserName,
            Email = DeletedUserEmail,
            EmailConfirmed = true,
            LockoutEnabled = false
        };

        var result = await userManager.CreateAsync(user, Guid.NewGuid().ToString("N") + "Aa1!");

        if (result.Succeeded)
            logger.LogInformation("Created system DeletedUser account");
        else
            logger.LogError("Failed to create DeletedUser: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
    }

    private static async Task SeedRoleAccountAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        ILogger logger,
        string userId,
        string username,
        string email,
        string password,
        string role)
    {
        var existing = await userManager.FindByIdAsync(userId);
        if (existing != null) return;

        var user = new ApplicationUser
        {
            Id = userId,
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = false
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            logger.LogError("Failed to create seed {Role} user: {Errors}", role, string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, role);

        var profile = new UserProfile
        {
            UserId = userId,
            FirstName = char.ToUpper(username[0]) + username[1..],
            LastName = "Seed",
            Bio = $"Default {role.ToLower()} seed account.",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Created seed {Role} account — username: '{Username}', password: '{Password}'", role, username, password);
    }

    public static string GetDeletedUserId() => DeletedUserId;
}