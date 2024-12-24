using Microsoft.EntityFrameworkCore;
using UnknownSanta.Domain.Entities;
using UnknownSanta.Infrastructure.DAL.Configurations;

namespace UnknownSanta.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Games> Games { get; set; }
        public DbSet<Users> Users { get; set; }
        
        public DbSet<SendMessage> SendMessages { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new GamesConfiguration());
            modelBuilder.ApplyConfiguration(new UsersConfiguration());
        }
    }
}