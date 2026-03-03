using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Quizzy.Api.Models;

namespace Quizzy.Api.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Quiz> Quizzes { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Choice> Choices { get; set; }
    public DbSet<QuizAttempt> QuizAttempts { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // UserProfile configuration (1:1 with ApplicationUser)
        builder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasOne(e => e.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<UserProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Category configuration
        builder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.IsDeleted);
        });

        // Quiz configuration
        builder.Entity<Quiz>(entity =>
        {
            entity.HasIndex(e => e.TeacherId);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.AccessCode);
            entity.HasIndex(e => e.IsDeleted);

            entity.HasOne(e => e.Teacher)
                .WithMany(t => t.CreatedQuizzes)
                .HasForeignKey(e => e.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Question configuration
        builder.Entity<Question>(entity =>
        {
            entity.HasIndex(e => e.QuizId);
            entity.HasIndex(e => e.OrderIndex);

            entity.HasOne(e => e.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(e => e.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Choice configuration
        builder.Entity<Choice>(entity =>
        {
            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => e.OrderIndex);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.Choices)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // QuizAttempt configuration
        builder.Entity<QuizAttempt>(entity =>
        {
            entity.HasIndex(e => e.QuizId);
            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => new { e.QuizId, e.StudentId });
            entity.HasIndex(e => new { e.QuizId, e.StudentId, e.Status });

            entity.HasOne(e => e.Quiz)
                .WithMany(q => q.QuizAttempts)
                .HasForeignKey(e => e.QuizId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Student)
                .WithMany(s => s.QuizAttempts)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StudentAnswer configuration
        builder.Entity<StudentAnswer>(entity =>
        {
            entity.HasIndex(e => e.AttemptId);
            entity.HasIndex(e => e.QuestionId);
            entity.HasIndex(e => new { e.AttemptId, e.QuestionId });

            entity.HasOne(e => e.Attempt)
                .WithMany(a => a.StudentAnswers)
                .HasForeignKey(e => e.AttemptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(e => e.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Choice)
                .WithMany(c => c.StudentAnswers)
                .HasForeignKey(e => e.ChoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Identity configuration
        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.HasKey(r => new { r.UserId, r.RoleId });
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.RoleId);
        });
    }
}
