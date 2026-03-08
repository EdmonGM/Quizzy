using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class ChoiceMappers
{
    public static ChoiceResponseDto ToChoiceResponseDto(this Choice choice)
    {
        return new ChoiceResponseDto
        {
            Id =  choice.Id,
            Content =  choice.Content,
            IsCorrect = choice.IsCorrect,
            OrderIndex = choice.OrderIndex,
        };
    }
    public static Choice ToChoice(this CreateChoiceDto dto, Guid questionId)
    {
        return new Choice
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Content = dto.Content,
            IsCorrect = dto.IsCorrect,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
    public static Choice ToChoice(this UpdateChoiceDto dto, Guid questionId)
    {
        return new Choice
        {
            Id = Guid.NewGuid(),
            QuestionId = questionId,
            Content = dto.Content,
            IsCorrect = dto.IsCorrect,
            OrderIndex = dto.OrderIndex,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateChoiceFromDto(this Choice choice, UpdateChoiceDto dto)
    {
        choice.Content = dto.Content;
        choice.IsCorrect = dto.IsCorrect;
        choice.OrderIndex = dto.OrderIndex;
        choice.UpdatedAt = DateTime.UtcNow;
    }
}