using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [Required]
    [MaxLength(128)]
    public string FirstName { get; set; } = string.Empty;
    [Required]
    [MaxLength(128)]
    public string LastName { get; set; } = string.Empty;
    [MaxLength(256)]
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
