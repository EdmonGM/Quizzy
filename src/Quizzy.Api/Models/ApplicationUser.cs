using Microsoft.AspNetCore.Identity;

namespace Quizzy.Api.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
    public UserProfile? Profile { get; set; }
    public ICollection<Quiz> CreatedQuizzes { get; set; } = new List<Quiz>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
}

public class ApplicationRole : IdentityRole
{
    public ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();
}
