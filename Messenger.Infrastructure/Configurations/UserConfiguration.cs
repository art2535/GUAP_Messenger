using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Messenger.Infrastructure.Configurations
{
    /// <summary>
    /// Конфигурация для сущности User
    /// </summary>
    internal class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Пользователи");

            builder.HasKey(u => u.Id);

            builder.Property(u => u.Id)
                .HasColumnName("ID_пользователя")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(u => u.AccountSettingsId)
                .HasColumnName("ID_аккаунта")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(u => u.LastName)
                .HasColumnName("Фамилия")
                .HasColumnType("varchar(50)")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.FirstName)
                .HasColumnName("Имя")
                .HasColumnType("varchar(50)")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(u => u.MiddleName)
                .HasColumnName("Отчество")
                .HasColumnType("varchar(50)")
                .HasMaxLength(50);

            builder.Property(u => u.BirthDate)
                .HasColumnName("Дата_рождения")
                .HasColumnType("date")
                .IsRequired();

            builder.Property(u => u.RegistrationDate)
                .HasColumnName("Дата_регистрации")
                .HasColumnType("date")
                .IsRequired();

            builder.Property(u => u.Login)
                .HasColumnName("Логин")
                .HasColumnType("varchar(20)")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(u => u.PasswordHash)
                .HasColumnName("Пароль")
                .HasColumnType("varchar(255)")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(u => u.Phone)
                .HasColumnName("Телефон")
                .HasColumnType("char(18)")
                .HasMaxLength(18)
                .IsRequired();

            builder.HasMany(user => user.Chats)
                .WithOne(chat => chat.Creator)
                .HasForeignKey(chat => chat.CreatorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.ChatParticipants)
                .WithOne(chatPart => chatPart.User)
                .HasForeignKey(chatPart => chatPart.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.SentMessages)
                .WithOne(message => message.Sender)
                .HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.ReceivedMessages)
                .WithOne(message => message.Receiver)
                .HasForeignKey(message => message.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.Reactions)
                .WithOne(reactions => reactions.User)
                .HasForeignKey(reactions => reactions.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.Notifications)
                .WithOne(notify => notify.User)
                .HasForeignKey(notify => notify.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.Logins)
                .WithOne(login => login.User)
                .HasForeignKey(login => login.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.BlockedUsers)
                .WithOne(blackList => blackList.User)
                .HasForeignKey(blackList => blackList.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(user => user.BlockedByUsers)
                .WithOne(blackList => blackList.BlockedUser)
                .HasForeignKey(blackList => blackList.BlockedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(messageStatus => messageStatus.MessageStatuses)
                .WithOne(user => user.User)
                .HasForeignKey(user => user.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(userStatus => userStatus.UserStatuses)
                .WithOne(user => user.User)
                .HasForeignKey(user => user.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}