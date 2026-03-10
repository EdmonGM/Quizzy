using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class UserProfileMapper
{
    public static UserProfileDto ToUserProfileDto(this UserProfile profile, string username, string email)
    {
        return new UserProfileDto
        {
            UserId = profile.UserId,
            Username = username,
            Email = email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Bio = profile.Bio,
            ProfileImageUrl = profile.ProfileImageUrl,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
