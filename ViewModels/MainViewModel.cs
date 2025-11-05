using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using static RecipeUI.MainWindow;

namespace RecipeUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;
        
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ShoppingRow> ShoppingList { get; } = new();

        public RelayCommand GenerateShoppingListCommand { get; }

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<RecipeInfo> RecipeListItems { get; } = new();

        public AddRecipeViewModel AddRecipeVM { get; }

        private RecipeInfo? _selectedRecipe;
        public RecipeInfo? SelectedRecipe
        {
            get => _selectedRecipe;
            set
            {
                if (_selectedRecipe != value)
                {
                    _selectedRecipe = value;
                    OnPropertyChanged(nameof(SelectedRecipe));
                    LoadSelectedRecipeDetails();
                }
            }
        }

        public string SelectedRecipeText { get; private set; } = string.Empty;
        public ObservableCollection<ReceptReszletRow> SelectedRecipeRows { get; } = new();
        public RelayCommand AddToSelectedCommand { get; }
        public RelayCommand RemoveSelectedCommand { get; }
        public RelayCommand OpenAddRecipeCommand { get; }

        public ObservableCollection<string> SelectedRecipes { get; } = new();

        public MainViewModel(IReceptSzerviz service)
        {
            _service = service;

            AddRecipeVM = new AddRecipeViewModel(service);

            AddToSelectedCommand = new RelayCommand(_ => AddSelectedRecipe());
            GenerateShoppingListCommand = new RelayCommand(_ => GenerateShoppingList());

            RemoveSelectedCommand = new RelayCommand(recipeName => RemoveSelectedRecipe(recipeName as string));

            OpenAddRecipeCommand = new RelayCommand(_ => OpenAddRecipe());

        }

        public void LoadRecipes()
        {
            RecipeListItems.Clear();

            var all = _service.ListazRecepteket();

            foreach (var rec in all)
            {
                var id = CreateShortID(rec.ReceptNev);
                RecipeListItems.Add(new RecipeInfo
                {
                    ID = id,
                    Name = rec.ReceptNev
                });
            }
        }

        private string CreateShortID(string nev)
        {
            if (string.IsNullOrWhiteSpace(nev))
                return "";

            var words = nev.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (words.Length >= 2)
                return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";

            if (words.Length == 1 && words[0].Length >= 2)
                return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[0][words[0].Length - 1])}";

            return nev.Substring(0, 1).ToUpper();
        }

        private void LoadSelectedRecipeDetails()
        {
            SelectedRecipeText = "";
            SelectedRecipeRows.Clear();

            if (SelectedRecipe == null) return;

            var recept = _service.ListazRecepteket()
                .FirstOrDefault(r => r.ReceptNev == SelectedRecipe.Name);

            if (recept == null) return;

            SelectedRecipeText = recept.Elkeszites;
            OnPropertyChanged(nameof(SelectedRecipeText));

            foreach (var h in recept.Hozzavalok.OrderBy(h => h.Alapanyag.AlapanyagNev))
            {
                SelectedRecipeRows.Add(new ReceptReszletRow
                {
                    Alapanyag = h.Alapanyag.AlapanyagNev,
                    Mennyiseg = h.SzükségesMennyiseg,
                    Mertekegyseg = h.Alapanyag.Mertekegyseg
                });
            }
        }

        private void AddSelectedRecipe()
        {
            if (SelectedRecipe == null) return;

            if (!SelectedRecipes.Contains(SelectedRecipe.Name))
                SelectedRecipes.Add(SelectedRecipe.Name);
        }

        private void GenerateShoppingList()
        {
            ShoppingList.Clear();

            if (SelectedRecipes.Count == 0)
                return;

            var allRecipes = _service.ListazRecepteket();
            var stock = _service.ListazRaktart();

            var selectedSet = new HashSet<string>(SelectedRecipes, StringComparer.OrdinalIgnoreCase);

            var requiredByIngredient = new Dictionary<int, (string Nev, string Mertekegyseg, decimal Ossz)>();

            foreach (var rec in allRecipes)
            {
                if (!selectedSet.Contains(rec.ReceptNev)) continue;

                foreach (var h in rec.Hozzavalok)
                {
                    var id = h.AlapanyagID;
                    var nev = h.Alapanyag.AlapanyagNev;
                    var mertek = h.Alapanyag.Mertekegyseg;
                    var qty = h.SzükségesMennyiseg;

                    if (requiredByIngredient.TryGetValue(id, out var agg))
                        requiredByIngredient[id] = (nev, mertek, agg.Ossz + qty);
                    else
                        requiredByIngredient[id] = (nev, mertek, qty);
                }
            }

            var stockByIngredient = stock.ToDictionary(r => r.AlapanyagID, r => r.Mennyiseg);

            foreach (var kvp in requiredByIngredient)
            {
                var id = kvp.Key;
                var (nev, mertek, need) = kvp.Value;
                var have = stockByIngredient.TryGetValue(id, out var s) ? s : 0m;
                var missing = need - have;
                if (missing > 0)
                {
                    ShoppingList.Add(new ShoppingRow
                    {
                        Alapanyag = nev,
                        HianyMennyiseg = missing,
                        Mertekegyseg = mertek
                    });
                }
            }
        }

        private void RemoveSelectedRecipe(string? recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName))
                return;

            SelectedRecipes.Remove(recipeName);
        }

        private void OpenAddRecipe()
        {
            // Ezt később kiváltjuk egy ViewService-el vagy Event-el.
            // Most csak jelezzük a MainWindow-nak, hogy nyissa meg a panelt.
            RecipeAddRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler? RecipeAddRequested;

    }

    public class RecipeInfo
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
