using AiNewsBot_Backend.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiNewsBot_Backend.Core.Data.Contexts;

public class PostsContext : DbContext
{
    public DbSet<Post> Posts { get; set; }
    
    public PostsContext(DbContextOptions<PostsContext> options) : base(options)
    {
    }
}