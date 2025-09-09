using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class UserStatusConfiguration : IEntityTypeConfiguration<UserStatus>
    {
        public void Configure(EntityTypeBuilder<UserStatus> builder)
        {
            builder.ToTable("Статусы_пользователей");

            builder.HasKey(status => status.UserId);

            builder.Property(status => status.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(status => status.IsOnline)
                .HasColumnName("Онлайн")
                .HasColumnType("boolean")
                .IsRequired();

            builder.Property(status => status.LastActivity)
                .HasColumnName("Последняя_активность")
                .HasColumnType("timestamp without time zone")
                .IsRequired();
        }
    }
}