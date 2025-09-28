using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models
{
    public class Recept
    {
        public int Id { get; set; }
        public string Nev { get; set; } = string.Empty;

        // Navigáció
        public ICollection<ReceptOsszetevo> Osszetevok { get; set; } = new List<ReceptOsszetevo>();
    }
}

