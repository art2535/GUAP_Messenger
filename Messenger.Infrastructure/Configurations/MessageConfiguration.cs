using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    /// <summary>
    /// Конфигурация для сущности Message
    /// </summary>
    internal class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            builder.ToTable("Сообщения");

            builder.HasKey(message => message.Id);

            builder.Property(message => message.Id)
                .HasColumnName("ID_сообщения")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(message => message.SenderId)
                .HasColumnName("ID_отправителя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(message => message.ReceiverId)
                .HasColumnName("ID_получателя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(message => message.ChatId)
                .HasColumnName("ID_чата")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(message => message.Text)
                .HasColumnName("Текст_сообщения")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(message => message.HasAttachments)
                .HasColumnName("Наличие_приложений")
                .HasColumnType("boolean")
                .IsRequired();

            builder.Property(message => message.SentAt)
                .HasColumnName("Время_отправки")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.HasMany(message => message.Attachments)
                .WithOne(attachment => attachment.Message)
                .HasForeignKey(attachment => attachment.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(message => message.Reactions)
                .WithOne(reactions => reactions.Message)
                .HasForeignKey(reactions => reactions.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(message => message.Statuses)
                .WithOne(status => status.Message)
                .HasForeignKey(status => status.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}