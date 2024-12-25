using Microsoft.EntityFrameworkCore;
using UnknownSanta.Domain.Entities;
using UnknownSanta.Infrastructure.DAL.Configurations;

namespace UnknownSanta.Infrastructure
{
    public class ApplicationDbContext : DbContext
    {
        // Свойство для работы с таблицей Games
        public DbSet<Games> Games { get; set; }
        
        // Свойство для работы с таблицей Users
        public DbSet<Users> Users { get; set; }
        
        // Свойство для работы с таблицей SendMessages
        public DbSet<SendMessage> SendMessages { get; set; }
        
        // Конструктор, принимающий параметры для настройки контекста
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        // Метод для конфигурации модели данных
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Вызов стандартной конфигурации базового класса
            base.OnModelCreating(modelBuilder);

            // Применение пользовательских конфигураций для сущностей
            modelBuilder.ApplyConfiguration(new GamesConfiguration()); // Конфигурация для сущности Games
            modelBuilder.ApplyConfiguration(new UsersConfiguration()); // Конфигурация для сущности Users
        }
    }
}