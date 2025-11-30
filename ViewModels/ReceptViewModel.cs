using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using static RecipeUI.MainWindow;

namespace RecipeUI.ViewModels
{
    public class ReceptViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? RecipeAddRequested;
        public event EventHandler? ShoppingListRequested;

        public AddRecipeViewModel AddRecipeVM { get; }

        public ObservableCollection<RecipeInfo> RecipeListItems { get; } = new();
        public ObservableCollection<ReceptReszletRow> SelectedRecipeRows { get; } = new();
        public ObservableCollection<string> SelectedRecipes { get; } = new();

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

        public RelayCommand GenerateShoppingListCommand { get; }
        public RelayCommand AddToSelectedCommand { get; }
        public RelayCommand RemoveSelectedCommand { get; }
        public RelayCommand OpenAddRecipeCommand { get; }
        public RelayCommand BeszerezveCommand { get; }
        public RelayCommand ElkeszitveCommand { get; }

        public ReceptViewModel(IReceptSzerviz service)
        {
            _service = service;

            AddRecipeVM = new AddRecipeViewModel(service);

            AddToSelectedCommand = new RelayCommand(_ => AddSelectedRecipe());
            GenerateShoppingListCommand = new RelayCommand(_ => GenerateShoppingList());
            RemoveSelectedCommand = new RelayCommand(n => RemoveSelectedRecipe(n as string));
            OpenAddRecipeCommand = new RelayCommand(_ => RecipeAddRequested?.Invoke(this, EventArgs.Empty));
            BeszerezveCommand = new RelayCommand(_ => MarkAsProcured());
            ElkeszitveCommand = new RelayCommand(_ => MarkAsPrepared());
        }

        #region Betöltések
        public void LoadRecipes()
        {
            RecipeListItems.Clear();

            var all = _service.ListazRecepteket();
            foreach (var rec in all)
            {
                var id = CreateShortID(rec.ReceptNev);
                RecipeListItems.Add(new RecipeInfo { ID = id, Name = rec.ReceptNev });
            }
        }

        private string CreateShortID(string nev)
        {
            if (string.IsNullOrWhiteSpace(nev)) return "";

            var words = nev.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
                return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";
            if (words.Length == 1 && words[0].Length >= 2)
                return $"{char.ToUpper(words[0][0])}{char.ToUpper(words[0][^1])}";

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
        #endregion

        #region Bevásárlólista / műveletek
        private void AddSelectedRecipe()
        {
            if (SelectedRecipe == null) return;
            if (!SelectedRecipes.Contains(SelectedRecipe.Name))
                SelectedRecipes.Add(SelectedRecipe.Name);
        }

        private void RemoveSelectedRecipe(string? recipeName)
        {
            if (string.IsNullOrWhiteSpace(recipeName)) return;
            SelectedRecipes.Remove(recipeName);
        }

        private void GenerateShoppingList()
        {
            ShoppingListRequested?.Invoke(this, EventArgs.Empty);
        }
        #endregion

        #region Készlet műveletek (beszerezve / elkészítve)
        private void MarkAsProcured()
        {
            if (SelectedRecipes.Count == 0)
            {
                MessageBox.Show("A jobb oldali listában nincs egy recept sem. Előbb adj hozzá recepteket!", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<Recept> allRecipes;
            List<Raktar> stock;
            try
            {
                allRecipes = _service.ListazRecepteket();
                stock = _service.ListazRaktart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az adatok betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSet = new HashSet<string>(SelectedRecipes, StringComparer.OrdinalIgnoreCase);
            var requiredByIngredient = new Dictionary<int, (string Nev, string Mertekegyseg, decimal Ossz)>();

            foreach (var rec in allRecipes)
            {
                if (!selectedSet.Contains(rec.ReceptNev)) continue;

                foreach (var h in rec.Hozzavalok)
                {
                    var id = h.AlapanyagID;
                    var nev = h.Alapanyag.AlapanyagNev;
                    var mer = h.Alapanyag.Mertekegyseg;
                    var qty = h.SzükségesMennyiseg;

                    if (requiredByIngredient.TryGetValue(id, out var agg))
                        requiredByIngredient[id] = (nev, mer, agg.Ossz + qty);
                    else
                        requiredByIngredient[id] = (nev, mer, qty);
                }
            }

            if (requiredByIngredient.Count == 0)
            {
                MessageBox.Show("A kijelölt recepteknek nincs hozzávalója.", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var stockByIngredient = stock.ToDictionary(r => r.AlapanyagID, r => r.Mennyiseg);
            var toAdd = new List<(int Id, decimal Qty)>();

            foreach (var kvp in requiredByIngredient)
            {
                var id = kvp.Key;
                var need = kvp.Value.Ossz;
                var have = stockByIngredient.TryGetValue(id, out var s) ? s : 0m;
                var missing = need - have;
                if (missing > 0m) toAdd.Add((id, missing));
            }

            if (toAdd.Count == 0)
            {
                MessageBox.Show("Minden hozzávaló megvan. Nincs mit hozzáadni a raktárhoz.", "Kész",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                SelectedRecipes.Clear();
                return;
            }

            try
            {
                foreach (var (id, qty) in toAdd)
                    _service.HozzaadRaktarhoz(id, qty);

                SelectedRecipes.Clear();
                MessageBox.Show("A bevásárlólista tételeit hozzáadtuk a Hűtő & Spájz készlethez.", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba a raktár frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MarkAsPrepared()
        {
            if (SelectedRecipes.Count == 0)
            {
                MessageBox.Show("A jobb oldali listában nincs egy recept sem. Előbb adj hozzá recepteket!", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<Recept> allRecipes;
            try
            {
                allRecipes = _service.ListazRecepteket();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a receptek betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSet = new HashSet<string>(SelectedRecipes, StringComparer.OrdinalIgnoreCase);
            var requiredByIngredient = new Dictionary<int, decimal>();

            foreach (var rec in allRecipes)
            {
                if (!selectedSet.Contains(rec.ReceptNev)) continue;

                foreach (var h in rec.Hozzavalok)
                {
                    var id = h.AlapanyagID;
                    var qty = h.SzükségesMennyiseg;

                    if (requiredByIngredient.ContainsKey(id))
                        requiredByIngredient[id] += qty;
                    else
                        requiredByIngredient[id] = qty;
                }
            }

            if (requiredByIngredient.Count == 0)
            {
                MessageBox.Show("A kijelölt receptekhez nem találtunk hozzávalókat.", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<Raktar> stockList;
            try
            {
                stockList = _service.ListazRaktart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a készlet betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var stockByIngredient = stockList.ToDictionary(r => r.AlapanyagID, r => r.Mennyiseg);

            try
            {
                foreach (var kvp in requiredByIngredient)
                {
                    var alapanyagId = kvp.Key;
                    var osszSzukseges = kvp.Value;

                    var have = stockByIngredient.TryGetValue(alapanyagId, out var current) ? current : 0m;
                    var consume = Math.Min(osszSzukseges, have);

                    if (consume > 0m)
                        _service.HozzaadRaktarhoz(alapanyagId, -consume);
                }

                SelectedRecipes.Clear();
                MessageBox.Show("A kiválasztott receptek hozzávalóit levontuk a Hűtő & Spájz készletből (negatív készlet nélkül).", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a raktár frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        private void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
