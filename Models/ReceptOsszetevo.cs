using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models
{
    public class ReceptOsszetevo
    {
        public int Id { get; set; }

        public int ReceptId { get; set; }
        public Recept Recept { get; set; } = null!;

        public int OsszetevoId { get; set; }
        public Osszetevo Osszetevo { get; set; } = null!;
    }
}
