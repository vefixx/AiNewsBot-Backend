using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace AiNewsBot_Backend.API.Models;

public class AnalyzePostBody
{
    [Required] public required string Text { get; set; }
}