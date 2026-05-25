using EnglishGame.Models;
using Microsoft.EntityFrameworkCore;

namespace EnglishGame.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<AiContent> AiContents => Set<AiContent>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Attempt> Attempts => Set<Attempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<AiContent>()
            .Property(x => x.ContentJson)
            .HasColumnType("nvarchar(max)");

        modelBuilder.Entity<AiContent>()
            .Property(x => x.RawResponse)
            .HasColumnType("nvarchar(max)");

        // ✅ GameSession relations (FK thật, không shadow)
        modelBuilder.Entity<GameSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameSession>()
            .HasOne(s => s.Topic)
            .WithMany()
            .HasForeignKey(s => s.TopicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<GameSession>()
            .HasOne(s => s.AiContent)
            .WithMany()
            .HasForeignKey(s => s.AiContentId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ Attempt -> GameSession
        modelBuilder.Entity<Attempt>()
            .HasOne(a => a.GameSession)
            .WithMany()
            .HasForeignKey(a => a.GameSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // ✅ tránh duplicate attempts cho cùng 1 câu trong 1 session
        modelBuilder.Entity<Attempt>()
            .HasIndex(a => new { a.GameSessionId, a.QuestionNo })
            .IsUnique();

        // (optional nhưng tốt) giới hạn length ở DB (khớp annotations)
        modelBuilder.Entity<Attempt>()
            .Property(a => a.UserAnswer)
            .HasMaxLength(2000);

        modelBuilder.Entity<Attempt>()
            .Property(a => a.Explanation)
            .HasMaxLength(2000);

        modelBuilder.Entity<AiContent>()
            .Property(a => a.RawPrompt)
            .HasMaxLength(4000);
    }
}
