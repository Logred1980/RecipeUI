using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models;

public class ReceptHozzavalo
{
    public int ReceptID { get; set; }
    public int AlapanyagID { get; set; }
    public decimal SzükségesMennyiseg { get; set; }

    // Navigációk
    public Recept Recept { get; set; } = null!;
    public Alapanyag Alapanyag { get; set; } = null!;
}
