namespace Quizzy.Api.Dtos;

public class StudentAnswerResultDto
{
    public Guid AnswerId { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionContent { get; set; } = string.Empty;
    public string SelectedChoice { get; set; } = string.Empty;
    public string? CorrectChoice { get; set; }
    public bool IsCorrect { get; set; }
    public int Points { get; set; }
    public int MaxPoints { get; set; }
}