using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models;

public class Alapanyag
{
    public int AlapanyagID { get; set; }
    public string AlapanyagNev { get; set; } = string.Empty;
    public string Mertekegyseg { get; set; } = string.Empty;

    // Navigációk
    public ICollection<Raktar> Raktarak { get; set; } = new List<Raktar>();
    public ICollection<ReceptHozzavalo> ReceptHozzavalok { get; set; } = new List<ReceptHozzavalo>();
}
