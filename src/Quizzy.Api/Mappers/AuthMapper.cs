using Quizzy.Api.Dtos;
using Quizzy.Api.Models;

namespace Quizzy.Api.Mappers;

public static class AuthMapper
{
    public static ApplicationUser ToApplicationUser(this RegisterDto dto)
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
                Username = user.UserName!,
                Email = user.Email!,
                Roles = roles
            };
        }

        public UserResponseDto ToUserResponseDto()
        {
            return new UserResponseDto
            {
                UserId = user.Id,
                Username = user.UserName!,
                Email = user.Email!
            };
        }
    }
}


