using Microsoft.EntityFrameworkCore;
using RecipeUI.Models;

namespace RecipeUI.Data;

public class RecipeDbContext : DbContext
{
    public RecipeDbContext(DbContextOptions<RecipeDbContext> options) : base(options) { }

    // Ez kell a migrációhoz
    public RecipeDbContext() { }

    public DbSet<Osszetevo> Osszetevok { get; set; }
    public DbSet<Recept> Receptek { get; set; }
    public DbSet<ReceptOsszetevo> ReceptOsszetevok { get; set; }
    public DbSet<RaktarTetel> RaktarTetelek { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=recipes.db");
        }
    }
}
