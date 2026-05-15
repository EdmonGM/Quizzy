using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

/// <summary>
/// Seeds two published quizzes owned by the seed Teacher account,
/// each with several questions and four choices per question.
/// </summary>
public static class QuizSeeder
{
    private static readonly Guid BiologyQuizId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    private static readonly Guid MathQuizId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    private static readonly Guid HistoryQuizId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    private static readonly Guid TechQuizId = Guid.Parse("20000000-0000-0000-0000-000000000004");

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("QuizSeeder");

        var existingQuizIds = await dbContext.Quizzes.Select(q => q.Id).ToListAsync();

        var quizzesToSeed = BuildQuizzes()
            .Where(q => !existingQuizIds.Contains(q.Id))
            .ToList();

        if (quizzesToSeed.Count == 0)
        {
            logger.LogInformation("Quizzes already seeded — skipping");
            return;
        }

        dbContext.Quizzes.AddRange(quizzesToSeed);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Seeded {Count} quizzes", quizzesToSeed.Count);
    }

    private static List<Quiz> BuildQuizzes() =>
    [
        BuildQuiz(
            id: BiologyQuizId,
            title: "Introduction to Biology",
            description: "Covers fundamental biology concepts including cells, genetics, and ecosystems.",
            categoryId: CategorySeeder.ScienceId,
            passingScore: 60,
            timeLimitMinutes: 15,
            maxAttempts: 3,
            accessCode: null,
            questions:
            [
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    content: "What is the powerhouse of the cell?",
                    orderIndex: 0,
                    points: 10,
                    choices:
                    [
                        ("Mitochondria", true),
                        ("Nucleus", false),
                        ("Ribosome", false),
                        ("Golgi apparatus", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    content: "Which molecule carries genetic information in most living organisms?",
                    orderIndex: 1,
                    points: 10,
                    choices:
                    [
                        ("DNA", true),
                        ("RNA", false),
                        ("ATP", false),
                        ("Protein", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000003"),
                    content: "What process do plants use to convert sunlight into energy?",
                    orderIndex: 2,
                    points: 10,
                    choices:
                    [
                        ("Photosynthesis", true),
                        ("Respiration", false),
                        ("Fermentation", false),
                        ("Transpiration", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000004"),
                    content: "How many chromosomes does a typical human cell contain?",
                    orderIndex: 3,
                    points: 10,
                    choices:
                    [
                        ("46", true),
                        ("23", false),
                        ("48", false),
                        ("92", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000005"),
                    content: "What is the basic unit of life?",
                    orderIndex: 4,
                    points: 10,
                    choices:
                    [
                        ("Cell", true),
                        ("Atom", false),
                        ("Organ", false),
                        ("Tissue", false),
                    ]),
            ]),

        BuildQuiz(
            id: MathQuizId,
            title: "Basic Mathematics",
            description: "Tests fundamental arithmetic, algebra, and geometry skills.",
            categoryId: CategorySeeder.MathematicsId,
            passingScore: 70,
            timeLimitMinutes: 20,
            maxAttempts: null,
            accessCode: null,
            questions:
            [
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000011"),
                    content: "What is the value of π (pi) to two decimal places?",
                    orderIndex: 0,
                    points: 5,
                    choices:
                    [
                        ("3.14", true),
                        ("3.41", false),
                        ("3.12", false),
                        ("3.16", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000012"),
                    content: "What is the square root of 144?",
                    orderIndex: 1,
                    points: 5,
                    choices:
                    [
                        ("12", true),
                        ("14", false),
                        ("11", false),
                        ("13", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000013"),
                    content: "If x + 5 = 12, what is x?",
                    orderIndex: 2,
                    points: 5,
                    choices:
                    [
                        ("7", true),
                        ("5", false),
                        ("17", false),
                        ("6", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000014"),
                    content: "How many degrees are in a right angle?",
                    orderIndex: 3,
                    points: 5,
                    choices:
                    [
                        ("90", true),
                        ("180", false),
                        ("45", false),
                        ("360", false),
                    ]),
            ]),

        BuildQuiz(
            id: HistoryQuizId,
            title: "World History: 20th Century",
            description: "Key events and figures from the twentieth century.",
            categoryId: CategorySeeder.HistoryId,
            passingScore: 60,
            timeLimitMinutes: 0,
            maxAttempts: 2,
            accessCode: "HIST2025",
            questions:
            [
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000021"),
                    content: "In which year did World War II end?",
                    orderIndex: 0,
                    points: 10,
                    choices:
                    [
                        ("1945", true),
                        ("1944", false),
                        ("1946", false),
                        ("1943", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000022"),
                    content: "Which country was the first to land humans on the Moon?",
                    orderIndex: 1,
                    points: 10,
                    choices:
                    [
                        ("United States", true),
                        ("Soviet Union", false),
                        ("United Kingdom", false),
                        ("France", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000023"),
                    content: "What year did the Berlin Wall fall?",
                    orderIndex: 2,
                    points: 10,
                    choices:
                    [
                        ("1989", true),
                        ("1991", false),
                        ("1987", false),
                        ("1985", false),
                    ]),
            ]),

        BuildQuiz(
            id: TechQuizId,
            title: "Programming Fundamentals",
            description: "Core programming concepts across languages and paradigms.",
            categoryId: CategorySeeder.TechnologyId,
            passingScore: 75,
            timeLimitMinutes: 10,
            maxAttempts: null,
            accessCode: null,
            questions:
            [
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000031"),
                    content: "What does HTTP stand for?",
                    orderIndex: 0,
                    points: 5,
                    choices:
                    [
                        ("HyperText Transfer Protocol", true),
                        ("HyperText Transmission Protocol", false),
                        ("High Transfer Text Protocol", false),
                        ("Hyperlink Text Transfer Protocol", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000032"),
                    content: "Which data structure operates on a LIFO (Last In, First Out) basis?",
                    orderIndex: 1,
                    points: 5,
                    choices:
                    [
                        ("Stack", true),
                        ("Queue", false),
                        ("Linked list", false),
                        ("Tree", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000033"),
                    content: "What is the time complexity of binary search?",
                    orderIndex: 2,
                    points: 10,
                    choices:
                    [
                        ("O(log n)", true),
                        ("O(n)", false),
                        ("O(n²)", false),
                        ("O(1)", false),
                    ]),
                BuildQuestion(
                    id: Guid.Parse("30000000-0000-0000-0000-000000000034"),
                    content: "In object-oriented programming, what is encapsulation?",
                    orderIndex: 3,
                    points: 10,
                    choices:
                    [
                        ("Bundling data and methods that operate on that data within a single unit", true),
                        ("A class inheriting properties from another class", false),
                        ("The ability of an object to take many forms", false),
                        ("Hiding the implementation of an interface", false),
                    ]),
            ]),
    ];

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static Quiz BuildQuiz(
        Guid id,
        string title,
        string description,
        Guid categoryId,
        int passingScore,
        int timeLimitMinutes,
        int? maxAttempts,
        string? accessCode,
        List<Question> questions)
    {
        var now = DateTime.UtcNow;
        return new Quiz
        {
            Id = id,
            Title = title,
            Description = description,
            TeacherId = UserSeeder.TeacherUserId,
            CategoryId = categoryId,
            PassingScore = passingScore,
            TimeLimitMinutes = timeLimitMinutes,
            MaxAttempts = maxAttempts,
            AccessCode = accessCode,
            IsPublished = true,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
            Questions = questions
        };
    }

    private static Question BuildQuestion(
        Guid id,
        string content,
        int orderIndex,
        int points,
        (string Content, bool IsCorrect)[] choices)
    {
        var now = DateTime.UtcNow;
        return new Question
        {
            Id = id,
            Content = content,
            OrderIndex = orderIndex,
            Points = points,
            CreatedAt = now,
            UpdatedAt = now,
            Choices = choices.Select((c, i) => new Choice
            {
                Id = Guid.NewGuid(),
                Content = c.Content,
                IsCorrect = c.IsCorrect,
                OrderIndex = i,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList()
        };
    }
}