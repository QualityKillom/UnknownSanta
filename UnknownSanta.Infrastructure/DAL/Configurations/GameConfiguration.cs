using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnknownSanta.Domain.Entities;

namespace UnknownSanta.Infrastructure.DAL.Configurations;

public class GamesConfiguration : IEntityTypeConfiguration<Games>
{
    public void Configure(EntityTypeBuilder<Games> builder)
    {
        // Установка первичного ключа
        builder.HasKey(g => g.Game_Id);

        // Настройка поля Game_Id как автоинкрементного
        builder.Property(g => g.Game_Id)
            .ValueGeneratedOnAdd();

        // Настройка строки Currency (ограничение длины)
        builder.Property(g => g.Currency)
            .IsRequired()
            .HasMaxLength(10); // например, USD или EUR

        // Настройка строки ChatType
        builder.Property(g => g.ChatType)
            .IsRequired()
            .HasMaxLength(20); // например, "Group", "Private"

        // Настройка суммы подарка
        builder.Property(g => g.Amount)
            .HasColumnType("decimal(18,2)");

        // Настройка связи с Users (один ко многим)
        builder.HasMany(g => g.Users)
            .WithOne() // Предполагаем, что в Users нет обратной ссылки на Games
            .HasForeignKey("Game_Id") // Внешний ключ в таблице Users
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка перечисления GameState
        builder.Property(g => g.GameState)
            .HasConversion<string>() // Сохранение в базе как строка
            .IsRequired();
    }
}