using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models;

public class Raktar
{
    public int AlapanyagID { get; set; }
    public decimal Mennyiseg { get; set; }

    // Navigáció
    public Alapanyag Alapanyag { get; set; } = null!;
}
