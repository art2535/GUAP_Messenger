using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
    {
        public void Configure(EntityTypeBuilder<MessageReaction> builder)
        {
            builder.ToTable("Реакции");

            builder.HasKey(reaction => reaction.Id);

            builder.Property(reaction => reaction.Id)
                .HasColumnName("ID_реакции")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(reaction => reaction.MessageId)
                .HasColumnName("ID_сообщения")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(reaction => reaction.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(reaction => reaction.ReactionType)
                .HasColumnName("Тип_реакции")
                .HasColumnType("varchar(30)")
                .IsRequired();
        }
    }
}