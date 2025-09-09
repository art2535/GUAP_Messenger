using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    internal class LoginSessionConfiguration : IEntityTypeConfiguration<LoginSession>
    {
        public void Configure(EntityTypeBuilder<LoginSession> builder)
        {
            builder.ToTable("Входы");

            builder.HasKey(login => login.Id);

            builder.Property(login => login.Id)
                .HasColumnName("ID_входа")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(login => login.UserId)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(login => login.Token)
                .HasColumnName("Токен")
                .HasColumnType("varchar(100)")
                .IsRequired();

            builder.Property(login => login.IpAddress)
                .HasColumnName("IP_адрес")
                .HasColumnType("varchar(15)")
                .IsRequired();

            builder.Property(login => login.LoginTime)
                .HasColumnName("Время_захода")
                .HasColumnType("timestamp without time zone")
                .IsRequired();

            builder.Property(login => login.LogoutTime)
                .HasColumnName("Время_выхода")
                .HasColumnType("timestamp without time zone");

            builder.Property(login => login.IsActive)
                .HasColumnName("Активен")
                .HasColumnType("boolean")
                .IsRequired();
        }
    }
}