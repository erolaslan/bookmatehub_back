using Microsoft.EntityFrameworkCore;
using BookMateHub.Api.Models;

namespace BookMateHub.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
    }
}
