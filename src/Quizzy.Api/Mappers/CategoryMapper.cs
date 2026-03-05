using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class CategoryMapper
{
    extension(Category category)
    {
        public CategoryResponseDto ToCategoryResponseDto(int quizCount = 0)
        {
            return new CategoryResponseDto
            {
                Id = category.Id,
                Name = category.Name,
                QuizCount = quizCount,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt
            };
        }
    }
}
