using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Dtos;

public class UserProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateUserProfileDto
{
    [MaxLength(128)]
    public string? FirstName { get; set; }
    [MaxLength(128)]
    public string? LastName { get; set; }
    [MaxLength(256)]
    public string? Bio { get; set; }
}

public class UpdateProfileImageDto
{
    [MaxLength(512)]
    public string? ProfileImageUrl { get; set; }
}
