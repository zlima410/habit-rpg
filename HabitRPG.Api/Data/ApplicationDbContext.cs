using Microsoft.EntityFrameworkCore;
using HabitRPG.Api.Models;

namespace HabitRPG.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Habit> Habits { get; set; } = null!;
        public DbSet<CompletionLog> CompletionLogs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User config
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Username).IsUnique();

                entity.Property(e => e.Id)
                    .HasColumnType("uuid")
                    .HasDefaultValueSql("gen_random_uuid()");

                entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Level).HasDefaultValue(1);
                entity.Property(e => e.XP).HasDefaultValue(0);
                entity.Property(e => e.TotalXP).HasDefaultValue(0);

                // PostgreSQL-specific timestamp configuration
                entity.Property(e => e.CreatedAt)
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("NOW()");
            });

            // Habit config
            modelBuilder.Entity<Habit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Frequency).HasConversion<int>();
                entity.Property(e => e.Difficulty).HasConversion<int>();
                entity.Property(e => e.CurrentStreak).HasDefaultValue(0);
                entity.Property(e => e.BestStreak).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.Property(e => e.CreatedAt)
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("NOW()");
                entity.Property(e => e.LastCompletedAt)
                      .HasColumnType("timestamp with time zone");

                entity.HasOne(h => h.User)
                      .WithMany(u => u.Habits)
                      .HasForeignKey(h => h.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // CompletionLog config
            modelBuilder.Entity<CompletionLog>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CompletedAt)
                      .HasColumnType("timestamp with time zone")
                      .HasDefaultValueSql("NOW()");

                entity.HasOne(cl => cl.Habit)
                      .WithMany(h => h.CompletionLogs)
                      .HasForeignKey(cl => cl.HabitId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(cl => new { cl.HabitId, cl.CompletedAt })
                      .HasDatabaseName("IX_HabitId_CompletedDate");
            });
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);
        }
    }
}