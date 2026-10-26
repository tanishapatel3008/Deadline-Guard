using DeadlineGuard.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DeadlineGuard.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Deadline> Deadlines => Set<Deadline>();
    public DbSet<DeadlineAttachment> DeadlineAttachments => Set<DeadlineAttachment>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeadlineHistory> DeadlineHistories => Set<DeadlineHistory>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser configuration
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.TimeZoneId).HasMaxLength(100).HasDefaultValue("UTC");
            entity.Property(u => u.PreferredTheme).HasMaxLength(20).HasDefaultValue("light");

            entity.HasOne(u => u.Preferences)
                  .WithOne(p => p.User)
                  .HasForeignKey<UserPreferences>(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Deadline configuration
        builder.Entity<Deadline>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Title).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Description).HasMaxLength(2000);
            entity.Property(d => d.CurrencyCode).HasMaxLength(5).HasDefaultValue("USD");
            entity.Property(d => d.PotentialPenaltyAmount).HasPrecision(18, 2);
            entity.Property(d => d.RowVersion).IsRowVersion();

            entity.HasOne(d => d.User)
                  .WithMany(u => u.Deadlines)
                  .HasForeignKey(d => d.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(d => d.UserId);
            entity.HasIndex(d => d.DueDateTime);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => new { d.UserId, d.Status });
            entity.HasIndex(d => new { d.UserId, d.DueDateTime });
            entity.HasIndex(d => new { d.UserId, d.Status, d.DueDateTime });
        });

        // DeadlineAttachment configuration
        builder.Entity<DeadlineAttachment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.Property(a => a.StoredFileName).HasMaxLength(255).IsRequired();
            entity.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();

            entity.HasOne(a => a.Deadline)
                  .WithMany(d => d.Attachments)
                  .HasForeignKey(a => a.DeadlineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.User)
                  .WithMany(u => u.Attachments)
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(a => a.DeadlineId);
            entity.HasIndex(a => a.UserId);
        });

        // Reminder configuration
        builder.Entity<Reminder>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.FailureReason).HasMaxLength(500);

            entity.HasOne(r => r.Deadline)
                  .WithMany(d => d.Reminders)
                  .HasForeignKey(r => r.DeadlineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reminders)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(r => r.ReminderTime);
            entity.HasIndex(r => new { r.ReminderTime, r.Status, r.IsEnabled });
            entity.HasIndex(r => new { r.DeadlineId, r.ReminderTime });
            entity.HasIndex(r => r.UserId);
        });

        // Notification configuration
        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Title).HasMaxLength(200).IsRequired();
            entity.Property(n => n.Message).HasMaxLength(1000).IsRequired();
            entity.Property(n => n.NotificationType).HasMaxLength(50).IsRequired();

            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Deadline)
                  .WithMany(d => d.Notifications)
                  .HasForeignKey(n => n.DeadlineId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(n => n.UserId);
            entity.HasIndex(n => new { n.UserId, n.ReadAt });
            entity.HasIndex(n => new { n.UserId, n.CreatedAt });
        });

        // DeadlineHistory configuration
        builder.Entity<DeadlineHistory>(entity =>
        {
            entity.HasKey(h => h.Id);

            entity.Property(h => h.Action).HasMaxLength(100).IsRequired();
            entity.Property(h => h.PreviousValue).HasMaxLength(500);
            entity.Property(h => h.NewValue).HasMaxLength(500);
            entity.Property(h => h.Notes).HasMaxLength(1000);

            entity.HasOne(h => h.Deadline)
                  .WithMany(d => d.Histories)
                  .HasForeignKey(h => h.DeadlineId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(h => h.User)
                  .WithMany(u => u.Histories)
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(h => h.DeadlineId);
            entity.HasIndex(h => new { h.DeadlineId, h.ChangedAt });
        });

        // UserPreferences configuration
        builder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.DefaultTimeZone).HasMaxLength(100).HasDefaultValue("UTC");
            entity.HasIndex(p => p.UserId).IsUnique();
        });
    }
}
