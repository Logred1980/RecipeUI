using Microsoft.EntityFrameworkCore;
using RecipeUI.Models;

namespace RecipeUI.Data;

public class RecipeDbContext : DbContext
{
    public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

    // paraméter nélküli konstruktor a migrációhoz
    public RecipeDbContext() { }

    public DbSet<Alapanyag> Alapanyagok { get; set; }
    public DbSet<Raktar> Raktarak { get; set; }
    public DbSet<Recept> Receptek { get; set; }
    public DbSet<ReceptHozzavalo> ReceptHozzavalok { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=recipes.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Alapanyag
        modelBuilder.Entity<Alapanyag>()
            .HasKey(a => a.AlapanyagID);

        modelBuilder.Entity<Alapanyag>()
            .HasIndex(a => a.AlapanyagNev)
            .IsUnique();

        // Raktár
        modelBuilder.Entity<Raktar>()
            .HasKey(r => r.AlapanyagID);

        modelBuilder.Entity<Raktar>()
            .HasOne(r => r.Alapanyag)
            .WithMany(a => a.Raktarak)
            .HasForeignKey(r => r.AlapanyagID)
            .OnDelete(DeleteBehavior.Cascade);

        // Recept
        modelBuilder.Entity<Recept>()
            .HasKey(r => r.ReceptID);

        // ReceptHozzavalo – összetett kulcs
        modelBuilder.Entity<ReceptHozzavalo>()
            .HasKey(rh => new { rh.ReceptID, rh.AlapanyagID });

        modelBuilder.Entity<ReceptHozzavalo>()
            .HasOne(rh => rh.Recept)
            .WithMany(r => r.Hozzavalok)
            .HasForeignKey(rh => rh.ReceptID)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ReceptHozzavalo>()
            .HasOne(rh => rh.Alapanyag)
            .WithMany(a => a.ReceptHozzavalok)
            .HasForeignKey(rh => rh.AlapanyagID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
