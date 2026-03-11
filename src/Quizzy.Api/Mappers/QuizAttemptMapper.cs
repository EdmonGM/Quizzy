using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class QuizAttemptMapper
{
    public static QuizAttemptSummaryDto ToQuizAttemptSummaryDto(this QuizAttempt attempt)
    {
        var percentage = attempt.TotalPossibleScore > 0
            ? (double)attempt.Score / attempt.TotalPossibleScore * 100
            : 0;

        return new QuizAttemptSummaryDto
        {
            Id = attempt.Id,
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            Score = attempt.Score,
            TotalPossibleScore = attempt.TotalPossibleScore,
            Percentage = Math.Round(percentage, 2),
            Passed = percentage >= 70,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            TimeSpentSeconds = attempt.TimeSpentSeconds
        };
    }

    public static QuizAttemptDto ToQuizAttemptDto(this QuizAttempt attempt)
    {
        var percentage = attempt.TotalPossibleScore > 0
            ? (double)attempt.Score / attempt.TotalPossibleScore * 100
            : 0;

        return new QuizAttemptDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            StudentId = attempt.StudentId,
            StudentName = attempt.Student.UserName!,
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            StartedAt = attempt.StartedAt,
            CompletedAt = attempt.CompletedAt,
            TimeSpentSeconds = attempt.TimeSpentSeconds,
            Score = attempt.Score,
            TotalPossibleScore = attempt.TotalPossibleScore,
            Percentage = Math.Round(percentage, 2),
            Passed = percentage >= 70,
            CreatedAt = attempt.CreatedAt,
            UpdatedAt = attempt.UpdatedAt
        };
    }

    public static QuizAttemptWithAnswersDto ToQuizAttemptWithAnswersDto(
        this QuizAttempt attempt,
        List<Question> questions,
        Dictionary<Guid, Guid> questionToAnswerMap,
        int? timeRemainingSeconds = null)
    {
        var quizQuestions = questions.OrderBy(q => q.OrderIndex).Select(q =>
        {
            var hasAnswer = questionToAnswerMap.TryGetValue(q.Id, out var answerId);
            return new QuestionWithUserAnswerDto
            {
                Id = q.Id,
                Content = q.Content,
                OrderIndex = q.OrderIndex,
                Points = q.Points,
                AnswerId = hasAnswer ? answerId : null,
                Choices = q.Choices.OrderBy(c => c.OrderIndex).Select(c => new ChoiceSummaryDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    OrderIndex = c.OrderIndex
                }).ToList()
            };
        }).ToList();

        return new QuizAttemptWithAnswersDto
        {
            Id = attempt.Id,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            AttemptNumber = attempt.AttemptNumber,
            Status = attempt.Status,
            TimeLimitMinutes = attempt.Quiz.TimeLimitMinutes,
            TimeRemainingSeconds = timeRemainingSeconds,
            StartedAt = attempt.StartedAt,
            TimeSpentSeconds = attempt.TimeSpentSeconds,
            Score = attempt.Score,
            TotalPossibleScore = attempt.TotalPossibleScore,
            Percentage = attempt.TotalPossibleScore > 0
                ? Math.Round((double)attempt.Score / attempt.TotalPossibleScore * 100, 2)
                : 0,
            Passed = attempt.TotalPossibleScore > 0
                ? (double)attempt.Score / attempt.TotalPossibleScore * 100 >= 70
                : false,
            Questions = quizQuestions
        };
    }

    public static SubmitAttemptResponseDto ToSubmitAttemptResponseDto(
        this QuizAttempt attempt,
        List<StudentAnswer> answers)
    {
        var percentage = attempt.TotalPossibleScore > 0
            ? (double)attempt.Score / attempt.TotalPossibleScore * 100
            : 0;

        var answerResults = answers.Select(a =>
        {
            var correctChoice = a.Question.Choices.FirstOrDefault(c => c.IsCorrect);
            return new StudentAnswerResultDto
            {
                QuestionId = a.QuestionId,
                QuestionContent = a.QuestionSnapshot,
                SelectedChoice = a.ChoiceSnapshot,
                CorrectChoice = correctChoice?.Content,
                IsCorrect = a.IsCorrect,
                Points = a.IsCorrect ? a.Question.Points : 0,
                MaxPoints = a.Question.Points
            };
        }).ToList();

        return new SubmitAttemptResponseDto
        {
            AttemptId = attempt.Id,
            Status = attempt.Status,
            Score = attempt.Score,
            TotalPossibleScore = attempt.TotalPossibleScore,
            Percentage = Math.Round(percentage, 2),
            Passed = percentage >= attempt.Quiz.PassingScore,
            CompletedAt = attempt.CompletedAt!.Value,
            TimeSpentSeconds = attempt.TimeSpentSeconds,
            Answers = answerResults
        };
    }

    public static QuizAttemptResultsDto ToQuizAttemptResultsDto(
        this QuizAttempt attempt,
        List<StudentAnswer> answers)
    {
        var baseResponse = attempt.ToSubmitAttemptResponseDto(answers);
        var percentage = attempt.TotalPossibleScore > 0
            ? (double)attempt.Score / attempt.TotalPossibleScore * 100
            : 0;

        return new QuizAttemptResultsDto
        {
            AttemptId = baseResponse.AttemptId,
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            AttemptNumber = attempt.AttemptNumber,
            Status = baseResponse.Status,
            Score = baseResponse.Score,
            TotalPossibleScore = baseResponse.TotalPossibleScore,
            Percentage = baseResponse.Percentage,
            Passed = percentage >= attempt.Quiz.PassingScore,
            PassingScore = attempt.Quiz.PassingScore,
            CompletedAt = baseResponse.CompletedAt,
            TimeSpentSeconds = baseResponse.TimeSpentSeconds,
            TimeLimitMinutes = attempt.Quiz.TimeLimitMinutes,
            StartedAt = attempt.StartedAt,
            Answers = baseResponse.Answers
        };
    }

    public static QuizAttemptsOverviewDto ToQuizAttemptsOverviewDto(
        this List<QuizAttempt> attempts,
        string quizTitle,
        Guid quizId)
    {
        var completed = attempts.Where(a => a.Status == QuizAttemptStatus.Completed).ToList();
        var inProgress = attempts.Where(a => a.Status == QuizAttemptStatus.InProgress).ToList();
        var abandoned = attempts.Where(a => a.Status == QuizAttemptStatus.Abandoned).ToList();

        var averageScore = completed.Count != 0
            ? completed.Average(a => a.TotalPossibleScore > 0
                ? (double)a.Score / a.TotalPossibleScore * 100
                : 0)
            : 0;

        var passRate = completed.Count != 0
            ? (double)completed.Count(a =>
            {
                var percentage = a.TotalPossibleScore > 0
                    ? (double)a.Score / a.TotalPossibleScore * 100
                    : 0;
                return percentage >= 70;
            }) / completed.Count * 100
            : 0;

        var averageTimeSpent = completed.Count != 0
            ? completed.Average(a => a.TimeSpentSeconds)
            : 0;

        var attemptDtos = attempts.Select(a =>
        {
            var percentage = a.TotalPossibleScore > 0
                ? (double)a.Score / a.TotalPossibleScore * 100
                : 0;

            return new TeacherQuizAttemptDto
            {
                Id = a.Id,
                Student = new StudentInfoDto
                {
                    Id = a.StudentId,
                    Name = a.Student.UserName!
                },
                AttemptNumber = a.AttemptNumber,
                Status = a.Status,
                Score = a.Score,
                TotalPossibleScore = a.TotalPossibleScore,
                Percentage = Math.Round(percentage, 2),
                Passed = percentage >= 70,
                StartedAt = a.StartedAt,
                CompletedAt = a.CompletedAt,
                TimeSpentSeconds = a.TimeSpentSeconds
            };
        }).ToList();

        return new QuizAttemptsOverviewDto
        {
            QuizId = quizId,
            QuizTitle = quizTitle,
            TotalAttempts = attempts.Count,
            CompletedAttempts = completed.Count,
            InProgressAttempts = inProgress.Count,
            AbandonedAttempts = abandoned.Count,
            AverageScore = Math.Round(averageScore, 2),
            PassRate = Math.Round(passRate, 2),
            AverageTimeSpentSeconds = Math.Round(averageTimeSpent, 2),
            Attempts = attemptDtos
        };
    }
    extension(QuizAttempt attempt)
    {
        public CreateAttemptResponseDto ToCreateAttemptResponseDto()
        {
            return new CreateAttemptResponseDto
            {
                AttemptId = attempt.Id,
                QuizId = attempt.QuizId,
                Status = attempt.Status,
                StartedAt = attempt.StartedAt
            };
        }

        public AbandonAttemptResponseDto ToAbandonAttemptResponseDto()
        {
            return new AbandonAttemptResponseDto
            {
                AttemptId = attempt.Id,
                AbandonedAt = attempt.CompletedAt!.Value
            };
        }
    }

    public static SubmitAnswerResponseDto ToSubmitAnswerResponseDto(this StudentAnswer answer)
    {
        return new SubmitAnswerResponseDto
        {
            AnswerId = answer.Id,
            Saved = true
        };
    }
}
