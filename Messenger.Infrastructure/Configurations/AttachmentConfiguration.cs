using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    /// <summary>
    /// Конфигурация для сущности Attachment
    /// </summary>
    internal class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("Вложения");

            builder.HasKey(attachment => attachment.Id);

            builder.Property(attachment => attachment.Id)
                .HasColumnName("ID_вложения")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(attachment => attachment.MessageId)
                .HasColumnName("ID_сообщения")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(attachment => attachment.FileName)
                .HasColumnName("Имя_файла")
                .HasColumnType("varchar(255)")
                .IsRequired();

            builder.Property(attachment => attachment.FileType)
                .HasColumnName("Тип_файла")
                .HasColumnType("varchar(100)");

            builder.Property(attachment => attachment.FileSize)
                .HasColumnName("Размер_файла")
                .HasColumnType("integer");

            builder.Property(attachment => attachment.Url)
                .HasColumnName("URL")
                .HasColumnType("text")
                .IsRequired();
        }
    }
}