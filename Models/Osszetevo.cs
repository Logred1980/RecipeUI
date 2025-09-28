using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models
{
    public class Osszetevo
    {
        public int Id { get; set; }
        public string Nev { get; set; } = string.Empty;

        // Navigáció
        public ICollection<ReceptOsszetevo> ReceptOsszetevok { get; set; } = new List<ReceptOsszetevo>();
        public ICollection<RaktarTetel> RaktarTetelek { get; set; } = new List<RaktarTetel>();
    }
}
