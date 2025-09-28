using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI.Models
{
    public class RaktarTetel
    {
        public int Id { get; set; }

        public int OsszetevoId { get; set; }
        public Osszetevo Osszetevo { get; set; } = null!;
    }
}
