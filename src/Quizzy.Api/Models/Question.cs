using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Models;

public class Question : BaseEntity
{
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    [Required]
    [MaxLength(128)]
    public string Content { get; set; } = string.Empty;
    [Required]
    public int OrderIndex { get; set; }
    [Required]
    public int Points { get; set; }
    public ICollection<Choice> Choices { get; set; } = new List<Choice>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
