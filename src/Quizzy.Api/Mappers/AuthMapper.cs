using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class AuthMapper
{
    public static ApplicationUser ToApplicationUser<T>(this T dto) where T : CreateAdminDto
    {
        return new ApplicationUser
        {
            UserName = dto.Username,
            Email = dto.Email,
            EmailConfirmed = false
        };
    }
    
    extension(ApplicationUser user)
    {
        public UserResponseDto ToUserResponseDto(IEnumerable<string> roles)
        {
            return new UserResponseDto
            {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles
            };
        }

        public UserResponseDto ToUserResponseDto()
        {
            return new UserResponseDto
            {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
            };
        }
    }
}