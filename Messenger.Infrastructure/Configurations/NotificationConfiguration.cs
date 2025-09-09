using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Уведомления");

            builder.HasKey(notify => notify.Id);

            builder.Property(notify => notify.Id)
                .HasColumnName("ID_уведомления")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(notify => notify.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(notify => notify.Text)
                .HasColumnName("Текст")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(notify => notify.CreatedAt)
                .HasColumnName("Дата_создания")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValue(DateTime.UtcNow)
                .IsRequired();

            builder.Property(notify => notify.IsRead)
                .HasColumnName("Прочитано")
                .HasColumnType("boolean")
                .HasDefaultValue(false)
                .IsRequired();
        }
    }
}