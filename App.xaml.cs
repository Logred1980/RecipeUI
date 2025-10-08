using System.Windows; 
using Microsoft.EntityFrameworkCore;
using RecipeUI.Data;

namespace RecipeUI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var options = new DbContextOptionsBuilder<RecipeDbContext>()
            .UseSqlite("Data Source=recipes.db")
            .Options;

        using var db = new RecipeDbContext(options);
        db.Database.Migrate();
    }
}
