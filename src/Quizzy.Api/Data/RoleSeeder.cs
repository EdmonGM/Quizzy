using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

public static class RoleSeeder
{
    private static readonly string[] FixedRoles = ["Admin", "Teacher", "Student"];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RoleSeeder");

        foreach (var roleName in FixedRoles)
        {
            if (await roleManager.RoleExistsAsync(roleName)) continue;
            var role = new ApplicationRole { Name = roleName };
            var result = await roleManager.CreateAsync(role);

            if (result.Succeeded)
            {
                logger.LogInformation("Created role: {Role}", roleName);
            }
            else
            {
                logger.LogError("Failed to create role {Role}: {Errors}", roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
