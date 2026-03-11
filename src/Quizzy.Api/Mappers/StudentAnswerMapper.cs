using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class StudentAnswerMapper
{
    public static StudentAnswerResultDto ToStudentAnswerResultDto(this StudentAnswer answer)
    {
        return new StudentAnswerResultDto
        {
            AnswerId = answer.Id,
            QuestionId = answer.QuestionId,
            QuestionContent = answer.QuestionSnapshot,
            SelectedChoice = answer.ChoiceSnapshot,
            CorrectChoice = answer.Question.Choices.FirstOrDefault(c => c.IsCorrect)?.Content,
            IsCorrect = answer.IsCorrect,
            Points = answer.IsCorrect ? answer.Question.Points : 0,
            MaxPoints = answer.Question.Points
        };
    }
}