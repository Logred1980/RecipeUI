using RecipeUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Services
{
    public interface IReceptSzerviz
    {
        // --- Alapanyag kezelés ---
        bool HozzaadAlapanyag(string nev, string mertekegyseg);
        List<Alapanyag> ListazAlapanyagok();

        // --- Raktár kezelés ---
        bool HozzaadRaktarhoz(int alapanyagId, decimal mennyiseg);
        List<Raktar> ListazRaktart();

        // --- Recept kezelés ---
        bool HozzaadRecept(string nev, string elkeszites, Dictionary<int, decimal> hozzavalok);
        List<Recept> ListazRecepteket();

        // --- Hiányzó tételek ---
        List<(string AlapanyagNev, decimal HianyMennyiseg, string Mertekegyseg)>
            LekerdezHianyzoHozzavalok(int receptId);
    }
}
