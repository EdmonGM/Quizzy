using System.ComponentModel.DataAnnotations;

namespace Quizzy.Api.Models;

public class Category : SoftDeleteEntity
{
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;
    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
}
