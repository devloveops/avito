using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AvitoBackend.Models;
using AvitoBackend.Models.Core;

namespace AvitoBackend.Data;

public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PromotionTransaction> PromotionTransactions => Set<PromotionTransaction>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // === Отношение: Advertisement → AppUser ===
        builder.Entity<Advertisement>()
            .HasOne(a => a.User)              
            .WithMany()                       
            .HasForeignKey(a => a.UserId)     
            .OnDelete(DeleteBehavior.Cascade);

        // === Отношение: Advertisement → Category ===
        builder.Entity<Advertisement>()
            .HasOne(a => a.Category)
            .WithMany(c => c.Advertisements)  
            .HasForeignKey(a => a.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // === Отношения для чатов ===
        builder.Entity<Chat>()
            .HasOne(c => c.Advertisement)
            .WithMany()
            .HasForeignKey(c => c.AdvertisementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Chat>()
            .HasOne(c => c.Buyer)             
            .WithMany()
            .HasForeignKey(c => c.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Отношения для сообщений ===
        builder.Entity<Message>()
            .HasOne(m => m.Chat)
            .WithMany()
            .HasForeignKey(m => m.ChatId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Message>()
            .HasOne(m => m.Sender)            
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // === Индексы ===
        builder.Entity<Advertisement>()
            .HasIndex(a => a.CreatedAt);
        builder.Entity<Advertisement>()
            .HasIndex(a => a.IsPromoted);
        builder.Entity<Advertisement>()
            .HasIndex(a => new { a.CategoryId, a.Price });

        builder.Entity<Category>()
            .HasIndex(c => c.ParentId);

        builder.Entity<Message>()
            .HasIndex(m => m.ChatId);
        builder.Entity<Message>()
            .HasIndex(m => m.SenderId);

        builder.Entity<Review>()
            .HasIndex(r => r.AdvertisementId);
        builder.Entity<Review>()
            .HasIndex(r => r.AuthorId);
        builder.Entity<Review>()
            .HasIndex(r => r.TargetUserId);
    }
}