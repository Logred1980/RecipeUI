using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models;

public class Recept
{
    public int ReceptID { get; set; }
    public string ReceptNev { get; set; } = string.Empty;
    public string Elkeszites { get; set; } = string.Empty;

    // Navigáció
    public ICollection<ReceptHozzavalo> Hozzavalok { get; set; } = new List<ReceptHozzavalo>();
}
