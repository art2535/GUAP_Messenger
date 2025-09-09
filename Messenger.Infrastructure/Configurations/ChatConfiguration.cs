using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class ChatConfiguration : IEntityTypeConfiguration<Chat>
    {
        public void Configure(EntityTypeBuilder<Chat> builder)
        {
            builder.ToTable("Чаты");
            
            builder.HasKey(chat => chat.Id);

            builder.Property(chat => chat.Id)
                .HasColumnName("ID_чата")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(chat => chat.Name)
                .HasColumnName("Название")
                .HasColumnType("varchar(100)")
                .IsRequired();

            builder.Property(chat => chat.Type)
                .HasColumnName("Тип")
                .HasColumnType("varchar(20)")
                .IsRequired();

            builder.Property(chat => chat.CreatorId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(chat => chat.CreatedAt)
                .HasColumnName("Дата_создания")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValue(DateTime.UtcNow)
                .IsRequired();

            builder.HasMany(chat => chat.Participants)
                .WithOne(chatPart => chatPart.Chat)
                .HasForeignKey(chatPart => chatPart.ChatId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(chat => chat.Messages)
                .WithOne(message => message.Chat)
                .HasForeignKey(message => message.ChatId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}