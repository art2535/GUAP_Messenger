using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class ChatParticipantConfiguration : IEntityTypeConfiguration<ChatParticipant>
    {
        public void Configure(EntityTypeBuilder<ChatParticipant> builder)
        {
            builder.ToTable("Участники_чата");

            builder.HasKey(chatPart => new { chatPart.UserId, chatPart.ChatId });

            builder.Property(chatPart => chatPart.ChatId)
                .HasColumnName("ID_чата")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(chatPart => chatPart.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(chatPart => chatPart.Role)
                .HasColumnName("Роль")
                .HasColumnType("varchar(20)")
                .HasDefaultValue("участник")
                .IsRequired();

            builder.Property(chatPart => chatPart.JoinedAt)
                .HasColumnName("Дата_вступления")
                .HasColumnType("timestamp without time zone")
                .HasDefaultValue(DateTime.UtcNow)
                .IsRequired();
        }
    }
}