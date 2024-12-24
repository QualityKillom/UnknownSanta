using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UnknownSanta.Domain.Entities;

namespace UnknownSanta.Infrastructure.DAL.Configurations
{
    public class SendMessageConfiguration : IEntityTypeConfiguration<SendMessage>
    {
        public void Configure(EntityTypeBuilder<SendMessage> builder)
        {
            builder.HasKey(sm => sm.Id);

            builder.Property(sm => sm.Telegram_Id)
                .IsRequired();

            builder.Property(sm => sm.SentAt)
                .IsRequired();

            builder.HasIndex(sm => sm.Telegram_Id)
                .HasDatabaseName("IX_SentMessages_TelegramId");
        }
    }
}