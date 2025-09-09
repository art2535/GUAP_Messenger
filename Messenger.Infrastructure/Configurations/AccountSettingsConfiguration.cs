using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    /// <summary>
    /// Конфигурация для сущности AccountSettings
    /// </summary>
    internal class AccountSettingsConfiguration : IEntityTypeConfiguration<AccountSettings>
    {
        public void Configure(EntityTypeBuilder<AccountSettings> builder)
        {
            builder.ToTable("Настройки_аккаунта");

            builder.HasKey(account => account.AccountId);

            builder.Property(account => account.AccountId)
                .HasColumnName("ID_настройки")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(account => account.AccountId)
                .HasColumnName("ID_аккаунта")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(account => account.Avatar)
                .HasColumnName("Аватар")
                .HasColumnType("text");

            builder.Property(account => account.Theme)
                .HasColumnName("Тема")
                .HasColumnType("varchar(15)")
                .HasMaxLength(15);

            builder.HasIndex(account => account.AccountId)
                .IsUnique();

            builder.HasOne(account => account.User)
                .WithOne(user => user.AccountSettings)
                .HasForeignKey<User>(user => user.Id)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}