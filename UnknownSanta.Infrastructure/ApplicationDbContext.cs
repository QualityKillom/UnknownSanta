using Microsoft.EntityFrameworkCore;
using UnknownSanta.Domain.Entities;

namespace UnknownSanta.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Games> Games { get; set; }
        public DbSet<Roles> Roles { get; set; }
        public DbSet<Users> Users { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
    }
}