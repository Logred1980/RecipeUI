using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RecipeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            FillData();
        }

        public void FillData()
        {
            CommonDatas.RecipeNameList = new List<string> { "Recipe1", "Recipe2", "Recipe3", "Recipe4", "Recipe5" };
            GenerateRecipeIDs();
            FillReceptListView();
        }

        public void FillReceptListView()
        {
            List<RecipeInfo> recipes = new List<RecipeInfo>();
            for (int i = 0; i < CommonDatas.RecipeNameList.Count; i++)
            {
                recipes.Add(new RecipeInfo
                {
                    ID = CommonDatas.RecipeIDList[i],
                    Name = CommonDatas.RecipeNameList[i]
                });
            }
            RecipeList.ItemsSource = recipes;
        }

        private void GenerateRecipeIDs()
        {
            if (CommonDatas.RecipeIDList != null) CommonDatas.RecipeIDList.Clear();

            string temporaryID = "";
            int helperSerialNr = 0;

            foreach (var item in CommonDatas.RecipeNameList)
            {
                string[] words = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Count() > 1)
                {
                    temporaryID = $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";
                }
                else
                {
                    temporaryID = $"{char.ToUpper(words[0][0])}{char.ToUpper(words[0][words[0].Length - 1])}";
                }
                if (CommonDatas.RecipeIDList != null)
                {
                    if (CommonDatas.RecipeIDList.Contains(temporaryID))
                    {
                        helperSerialNr++;
                        temporaryID += helperSerialNr.ToString();
                    }
                }
                CommonDatas.RecipeIDList.Add(temporaryID);
            }
        }
    }
}