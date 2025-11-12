using Messenger.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Messenger.Infrastructure.Data;

public partial class GuapMessengerContext : DbContext
{
    public GuapMessengerContext()
    {
    }

    public GuapMessengerContext(DbContextOptions<GuapMessengerContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountSetting> AccountSettings { get; set; }

    public virtual DbSet<Attachment> Attachments { get; set; }

    public virtual DbSet<Blacklist> Blacklists { get; set; }

    public virtual DbSet<Chat> Chats { get; set; }

    public virtual DbSet<ChatParticipant> ChatParticipants { get; set; }

    public virtual DbSet<Login> Logins { get; set; }

    public virtual DbSet<Message> Messages { get; set; }

    public virtual DbSet<MessageStatus> MessageStatuses { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Reaction> Reactions { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserStatus> UserStatuses { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Database=GUAP_Messenger;Username=postgres;Password=1234");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<AccountSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("Account_Settings_pkey");

            entity.Property(e => e.SettingId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Attachment>(entity =>
        {
            entity.HasKey(e => e.AttachmentId).HasName("Attachments_pkey");

            entity.Property(e => e.AttachmentId).ValueGeneratedNever();

            entity.HasOne(d => d.Message).WithMany(p => p.Attachments).HasConstraintName("fk_attachment_message");
        });

        modelBuilder.Entity<Blacklist>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.BlockedUserId }).HasName("Blacklist_pkey");

            entity.Property(e => e.BlockDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.BlockedUser).WithMany(p => p.BlacklistBlockedUsers).HasConstraintName("fk_blocked_to");

            entity.HasOne(d => d.User).WithMany(p => p.BlacklistUsers).HasConstraintName("fk_blocked_by");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.ChatId).HasName("Chats_pkey");

            entity.Property(e => e.ChatId).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.User).WithMany(p => p.Chats).HasConstraintName("fk_chat_creator");
        });

        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(e => new { e.ChatId, e.UserId }).HasName("Chat_Participants_pkey");

            entity.Property(e => e.JoinDate).HasDefaultValueSql("now()");
            entity.Property(e => e.Role).HasDefaultValueSql("'participant'::character varying");

            entity.HasOne(d => d.Chat).WithMany(p => p.ChatParticipants).HasConstraintName("fk_participant_chat");

            entity.HasOne(d => d.User).WithMany(p => p.ChatParticipants).HasConstraintName("fk_participant_user");
        });

        modelBuilder.Entity<Login>(entity =>
        {
            entity.HasKey(e => e.LoginId).HasName("Logins_pkey");

            entity.Property(e => e.LoginId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.Logins).HasConstraintName("fk_login_user");
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.MessageId).HasName("Messages_pkey");

            entity.Property(e => e.MessageId).ValueGeneratedNever();

            entity.HasOne(d => d.Chat).WithMany(p => p.Messages).HasConstraintName("fk_chat_messages");

            entity.HasOne(d => d.Recipient).WithMany(p => p.MessageRecipients).HasConstraintName("fk_recipient");

            entity.HasOne(d => d.Sender).WithMany(p => p.MessageSenders).HasConstraintName("fk_sender");
        });

        modelBuilder.Entity<MessageStatus>(entity =>
        {
            entity.HasKey(e => new { e.MessageId, e.UserId }).HasName("Message_Statuses_pkey");

            entity.Property(e => e.ChangeDate).HasDefaultValueSql("now()");

            entity.HasOne(d => d.Message).WithMany(p => p.MessageStatuses).HasConstraintName("fk_status_message");

            entity.HasOne(d => d.User).WithMany(p => p.MessageStatuses).HasConstraintName("fk_status_user2");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("Notifications_pkey");

            entity.Property(e => e.NotificationId).ValueGeneratedNever();
            entity.Property(e => e.CreationDate).HasDefaultValueSql("now()");
            entity.Property(e => e.Read).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany(p => p.Notifications).HasConstraintName("fk_notification_user");
        });

        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasKey(e => e.ReactionId).HasName("Reactions_pkey");

            entity.Property(e => e.ReactionId).ValueGeneratedNever();

            entity.HasOne(d => d.Message).WithMany(p => p.Reactions).HasConstraintName("fk_reaction_message");

            entity.HasOne(d => d.User).WithMany(p => p.Reactions).HasConstraintName("fk_reaction_user");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("Roles_pkey");

            entity.Property(e => e.RoleId).ValueGeneratedNever();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("Users_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.Phone).IsFixedLength();

            entity.HasOne(d => d.Account).WithMany(p => p.Users)
                .HasPrincipalKey(p => p.AccountId)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("fk_account");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_userrole_role"),
                    l => l.HasOne<User>().WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_userrole_user"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId").HasName("UserRoles_pkey");
                        j.ToTable("User_Roles");
                        j.IndexerProperty<Guid>("UserId").HasColumnName("User_ID");
                        j.IndexerProperty<Guid>("RoleId").HasColumnName("Role_ID");
                    });
        });

        modelBuilder.Entity<UserStatus>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("User_Statuses_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithOne(p => p.UserStatus).HasConstraintName("fk_status_user");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
