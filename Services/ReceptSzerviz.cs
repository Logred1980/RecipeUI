using RecipeUI.Data;
using RecipeUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace RecipeUI.Services
{
    class ReceptSzerviz : IReceptSzerviz
    {
        private readonly RecipeDbContext _context;

        public ReceptSzerviz(RecipeDbContext context)
        {
            _context = context;
        }

        // --- Alapanyag kezelés ---
        public bool HozzaadAlapanyag(string nev, string mertekegyseg)
        {
            if (_context.Alapanyagok.Any(a => a.AlapanyagNev == nev))
                return false;

            var uj = new Alapanyag
            {
                AlapanyagNev = nev,
                Mertekegyseg = mertekegyseg
            };

            _context.Alapanyagok.Add(uj);
            _context.SaveChanges();
            return true;
        }

        public List<Alapanyag> ListazAlapanyagok()
        {
            return _context.Alapanyagok.OrderBy(a => a.AlapanyagNev).ToList();
        }

        // --- Raktár kezelés ---
        public bool HozzaadRaktarhoz(int alapanyagId, decimal mennyiseg)
        {
            var raktar = _context.Raktarak.FirstOrDefault(r => r.AlapanyagID == alapanyagId);
            if (raktar == null)
            {
                raktar = new Raktar { AlapanyagID = alapanyagId, Mennyiseg = mennyiseg };
                _context.Raktarak.Add(raktar);
            }
            else
            {
                raktar.Mennyiseg += mennyiseg;
                _context.Raktarak.Update(raktar);
            }

            _context.SaveChanges();
            return true;
        }

        public List<Raktar> ListazRaktart()
        {
            return _context.Raktarak.Include(r => r.Alapanyag).OrderBy(r => r.Alapanyag.AlapanyagNev).ToList();
        }

        // --- Recept kezelés ---
        public bool HozzaadRecept(string nev, string elkeszites, Dictionary<int, decimal> hozzavalok)
        {
            if (_context.Receptek.Any(r => r.ReceptNev == nev))
                return false;

            var recept = new Recept
            {
                ReceptNev = nev,
                Elkeszites = elkeszites
            };

            foreach (var par in hozzavalok)
            {
                recept.Hozzavalok.Add(new ReceptHozzavalo
                {
                    AlapanyagID = par.Key,
                    SzükségesMennyiseg = par.Value
                });
            }

            _context.Receptek.Add(recept);
            _context.SaveChanges();
            return true;
        }

        public List<Recept> ListazRecepteket()
        {
            return _context.Receptek
                .Include(r => r.Hozzavalok)
                .ThenInclude(h => h.Alapanyag)
                .OrderBy(r => r.ReceptNev)
                .ToList();
        }

        // --- Hiányzó hozzávalók ---
        public List<(string AlapanyagNev, decimal HianyMennyiseg, string Mertekegyseg)>
            LekerdezHianyzoHozzavalok(int receptId)
        {
            var recept = _context.Receptek
                .Include(r => r.Hozzavalok)
                .ThenInclude(h => h.Alapanyag)
                .FirstOrDefault(r => r.ReceptID == receptId);

            if (recept == null)
                return new List<(string, decimal, string)>();

            var eredmeny = new List<(string, decimal, string)>();

            foreach (var h in recept.Hozzavalok)
            {
                var raktarTetel = _context.Raktarak.FirstOrDefault(r => r.AlapanyagID == h.AlapanyagID);
                var aktualis = raktarTetel?.Mennyiseg ?? 0;
                var hiany = h.SzükségesMennyiseg - aktualis;

                if (hiany > 0)
                    eredmeny.Add((h.Alapanyag.AlapanyagNev, hiany, h.Alapanyag.Mertekegyseg));
            }

            return eredmeny;
        }
    }
}
