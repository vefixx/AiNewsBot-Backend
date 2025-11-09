using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AiNewsBot_Backend.Core.Data.Entities;

[Table("Posts")]
public class Post
{
    public string PostId { get; set; }
    public string SourceText { get; set; }
    public string AiText { get; set; }
    public bool IsPublished { get; set; } = false;
    [Key] public ulong Id { get; set; }
}