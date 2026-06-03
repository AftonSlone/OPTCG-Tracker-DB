using Microsoft.EntityFrameworkCore;
using OPTCG.Tracker.Data.Models;

namespace OPTCG.Tracker.Data.Data;

public class TrackerDbContext : DbContext
{
    public TrackerDbContext(DbContextOptions<TrackerDbContext> options) : base(options)
    {
    }

    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(entity =>
        {
            entity.ToTable("cards");

            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.CardSetId).HasMaxLength(50).IsRequired();
            entity.Property(c => c.CardName).HasMaxLength(255).IsRequired();
            entity.Property(c => c.SetName).HasMaxLength(255);
            entity.Property(c => c.SetId).HasMaxLength(50);
            entity.Property(c => c.CardType).HasMaxLength(50);
            entity.Property(c => c.CardColor).HasMaxLength(100);
            entity.Property(c => c.Rarity).HasMaxLength(10);
            entity.Property(c => c.Attribute).HasMaxLength(50);
            entity.Property(c => c.SubTypes);
            entity.Property(c => c.CardText);
            entity.Property(c => c.CardImageUrl).HasMaxLength(255);
            entity.Property(c => c.CardImageId).HasMaxLength(100).IsRequired();

            // CardImageId is the true unique natural key (base + parallel printings differ here).
            entity.HasIndex(c => c.CardImageId).IsUnique();

            // CardSetId is NOT unique (base + parallel share it) - index only for lookups.
            entity.HasIndex(c => c.CardSetId);
            entity.HasIndex(c => c.CardName);
            entity.HasIndex(c => c.CardType);
            entity.HasIndex(c => c.CardColor);
            entity.HasIndex(c => c.Rarity);
            entity.HasIndex(c => c.SetId);
        });
    }
}
