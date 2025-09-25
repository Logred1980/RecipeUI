using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecipeUI
{
    class CommonDatas
    {
        public static List<string> RecipeNameList = new List<string>();
        public static List<string> RecipeIDList = new List<string>();
    }

    public class RecipeInfo
    {
        public string ID { get; set; }
        public string Name { get; set; }
    }
}
