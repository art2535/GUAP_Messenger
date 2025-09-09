using Messenger.Core.Models;
using Messenger.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Data
{
    public class GUAPMessengerContext : DbContext
    {
        public DbSet<AccountSettings> AccountSettings { get; set; }
        public DbSet<Attachment> Attachments { get; set; }
        public DbSet<BlackList> BlackLists { get; set; }
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatParticipant> ChatParticipants { get; set; }
        public DbSet<LoginSession> LoginSessions { get; set; }
        public DbSet<Message> Message { get; set; }
        public DbSet<MessageReaction> MessageReactions { get; set; }
        public DbSet<MessageStatus> MessageStatuses { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserStatus> UserStatuses { get; set; }

        public GUAPMessengerContext() { }

        public GUAPMessengerContext(DbContextOptions<GUAPMessengerContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new AccountSettingsConfiguration());
            modelBuilder.ApplyConfiguration(new AttachmentConfiguration());
            modelBuilder.ApplyConfiguration(new MessageConfiguration());
            modelBuilder.ApplyConfiguration(new BlackListConfiguration());
            modelBuilder.ApplyConfiguration(new ChatConfiguration());
            modelBuilder.ApplyConfiguration(new ChatParticipantConfiguration());
            modelBuilder.ApplyConfiguration(new LoginSessionConfiguration());
            modelBuilder.ApplyConfiguration(new MessageReactionConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new UserStatusConfiguration());

            modelBuilder.Entity<AccountSettings>().ToTable("Настройки_аккаунта");
            modelBuilder.Entity<User>().ToTable("Пользователи");
            modelBuilder.Entity<LoginSession>().ToTable("Входы");
            modelBuilder.Entity<Attachment>().ToTable("Вложения");
            modelBuilder.Entity<MessageReaction>().ToTable("Реакции");
            modelBuilder.Entity<Message>().ToTable("Сообщения");
            modelBuilder.Entity<UserStatus>().ToTable("Статусы_пользователей");
            modelBuilder.Entity<MessageStatus>().ToTable("Статусы_сообщений");
            modelBuilder.Entity<Notification>().ToTable("Уведомления");
            modelBuilder.Entity<Chat>().ToTable("Чаты");
            modelBuilder.Entity<ChatParticipant>().ToTable("Участники_чата");
            modelBuilder.Entity<BlackList>().ToTable("Черный_список");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=GUAP_Messenger;Username=postgres;Password=12345;");
            }
        }
    }
}