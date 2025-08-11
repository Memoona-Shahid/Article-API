using Microsoft.EntityFrameworkCore;
using WebApi1.Model;

namespace WebApi1.Data
{
    public class ArticleDbContext : DbContext
    {
        public ArticleDbContext(DbContextOptions<ArticleDbContext> options)
            : base(options)
        {
        }

        public DbSet<ArticleData> ArticleData { get; set; }

        public DbSet<User> Users { get; set; }
        
    }
}
