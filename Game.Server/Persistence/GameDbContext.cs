using Microsoft.EntityFrameworkCore;

namespace Game.Server.Persistence;

public class GameDbContext : DbContext
{
    public DbSet<AccountRecord> Accounts => Set<AccountRecord>();
    public DbSet<CharacterRecord> Characters => Set<CharacterRecord>();
    public DbSet<ItemRecord> Items => Set<ItemRecord>();

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AccountRecord>(e =>
        {
            e.HasIndex(a => a.Username).IsUnique();
            e.HasMany(a => a.Characters)
             .WithOne()
             .HasForeignKey(c => c.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<CharacterRecord>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.HasMany(c => c.Items)
             .WithOne()
             .HasForeignKey(i => i.CharacterId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<ItemRecord>(e =>
        {
            e.HasIndex(i => i.InstanceId);
            // Rolled attributes persist as a single JSON column on the item row.
            // Adding a new AttributeType never needs a migration this way.
            e.OwnsMany(i => i.Attributes, a => a.ToJson());
        });
    }
}
