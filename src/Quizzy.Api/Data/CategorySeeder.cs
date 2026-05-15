using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

public static class CategorySeeder
{
    public static readonly Guid ScienceId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    public static readonly Guid MathematicsId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    public static readonly Guid HistoryId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    public static readonly Guid TechnologyId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    public static readonly Guid LiteratureId = Guid.Parse("10000000-0000-0000-0000-000000000005");

    private static readonly List<Category> Categories =
    [
        new Category { Id = ScienceId,    Name = "Science" },
        new Category { Id = MathematicsId, Name = "Mathematics" },
        new Category { Id = HistoryId,    Name = "History" },
        new Category { Id = TechnologyId, Name = "Technology" },
        new Category { Id = LiteratureId, Name = "Literature" },
    ];

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CategorySeeder");

        var existingIds = await dbContext.Categories
            .Select(c => c.Id)
            .ToListAsync();

        var toAdd = Categories
            .Where(c => !existingIds.Contains(c.Id))
            .Select(c =>
            {
                c.CreatedAt = DateTime.UtcNow;
                c.UpdatedAt = DateTime.UtcNow;
                return c;
            })
            .ToList();

        if (toAdd.Count == 0)
        {
            logger.LogInformation("Categories already seeded — skipping");
            return;
        }

        dbContext.Categories.AddRange(toAdd);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} categories", toAdd.Count);
    }
}