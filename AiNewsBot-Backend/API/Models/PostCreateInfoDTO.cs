using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AiNewsBot_Backend.API.Models;

public class PostCreateInfoDTO
{
    [Required] public required string PostId { get; set; }
    [Required] public required string Text { get; set; }
}