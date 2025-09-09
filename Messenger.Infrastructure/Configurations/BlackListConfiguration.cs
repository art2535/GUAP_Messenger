using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class BlackListConfiguration : IEntityTypeConfiguration<BlackList>
    {
        public void Configure(EntityTypeBuilder<BlackList> builder)
        {
            builder.ToTable("Черный_список");

            builder.HasKey(list => new { list.UserId, list.BlockedUserId });

            builder.Property(list => list.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(list => list.BlockedUserId)
                .HasColumnName("ID_заблокированного")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(list => list.BlockedAt)
                .HasColumnName("Дата_блокировки")
                .HasColumnType("timestamp without time zone")
                .IsRequired();
        }
    }
}