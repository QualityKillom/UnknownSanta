using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnknownSanta.Domain.Entities;

namespace UnknownSanta.Infrastructure.DAL.Configurations;

public class UsersConfiguration : IEntityTypeConfiguration<Users>
{
    public void Configure(EntityTypeBuilder<Users> builder)
    {
        // Установка первичного ключа
        builder.HasKey(u => u.Id);

        // Настройка автоинкремента для Id
        builder.Property(u => u.Id)
            .ValueGeneratedOnAdd();

        // Настройка строки TagUserName
        builder.Property(u => u.TagUserName)
            .HasMaxLength(50) // Ограничение длины строки
            .IsRequired(false); // Поле необязательно

        // Поле Participle (участие в игре)
        builder.Property(u => u.Participle)
            .IsRequired();

        // Поле Game_Id (внешний ключ)
        builder.Property(u => u.Game_Id)
            .IsRequired();

        // Настройка связи с Games
        builder.HasOne<Games>() // Указываем связь с Games
            .WithMany(g => g.Users) // Связь "многие к одному" через коллекцию Users в Games
            .HasForeignKey(u => u.Game_Id) // Указываем внешний ключ
            .OnDelete(DeleteBehavior.Cascade); // Удаление пользователей при удалении игры

        // Поле Telegram_Id
        builder.Property(u => u.Telegram_Id)
            .IsRequired();
    }
}
