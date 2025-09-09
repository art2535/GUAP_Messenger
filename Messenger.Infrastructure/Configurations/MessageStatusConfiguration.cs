using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class MessageStatusConfiguration : IEntityTypeConfiguration<MessageStatus>
    {
        public void Configure(EntityTypeBuilder<MessageStatus> builder)
        {
            builder.ToTable("Статусы_сообщений");

            builder.HasKey(status => new { status.MessageId, status.UserId });

            builder.Property(status => status.MessageId)
                .HasColumnName("ID_сообщения")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(status => status.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(status => status.Status)
                .HasColumnName("Статус")
                .HasColumnType("varchar(20)")
                .IsRequired();

            builder.Property(status => status.UpdatedAt)
                .HasColumnName("Дата_изменения")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValue(DateTime.UtcNow)
                .IsRequired();
        }
    }
}