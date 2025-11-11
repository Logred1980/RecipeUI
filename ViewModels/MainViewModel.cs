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
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event EventHandler? RecipeAddRequested;
        public ObservableCollection<ShoppingRow> ShoppingList { get; } = new();

        public ReceptViewModel ReceptVM { get; }
        public HozzavalokViewModel HozzavalokVM { get; }
        public RaktarViewModel RaktarVM { get; }

        #region === Konstruktor ===
        public MainViewModel(IReceptSzerviz service)
        {
            _service = service;

            ReceptVM = new ReceptViewModel(service);
            HozzavalokVM = new HozzavalokViewModel(service);
            RaktarVM = new RaktarViewModel(service);

            ReceptVM.RecipeAddRequested += (_, __) => RecipeAddRequested?.Invoke(this, EventArgs.Empty);
            ReceptVM.ShoppingListRequested += (_, __) => RebuildShoppingList();
        }
        #endregion

        #region === Pass-through: Receptek fül (változatlan API a nézet felé) ===
        public ObservableCollection<RecipeInfo> RecipeListItems => ReceptVM.RecipeListItems;
        public ObservableCollection<ReceptReszletRow> SelectedRecipeRows => ReceptVM.SelectedRecipeRows;
        public ObservableCollection<string> SelectedRecipes => ReceptVM.SelectedRecipes;
        public string SelectedRecipeText { get => ReceptVM.SelectedRecipeText; }
        public RecipeInfo? SelectedRecipe
        {
            get => ReceptVM.SelectedRecipe;
            set => ReceptVM.SelectedRecipe = value;
        }

        public RelayCommand AddToSelectedCommand => ReceptVM.AddToSelectedCommand;
        public RelayCommand RemoveSelectedCommand => ReceptVM.RemoveSelectedCommand;
        public RelayCommand GenerateShoppingListCommand => ReceptVM.GenerateShoppingListCommand;
        public RelayCommand BeszerezveCommand => ReceptVM.BeszerezveCommand;
        public RelayCommand ElkeszitveCommand => ReceptVM.ElkeszitveCommand;
        public RelayCommand OpenAddRecipeCommand => ReceptVM.OpenAddRecipeCommand;
        public AddRecipeViewModel AddRecipeVM => ReceptVM.AddRecipeVM;

        public void LoadRecipes() => ReceptVM.LoadRecipes();
        #endregion

        #region === Pass-through: Hozzávalók fül ===
        public ObservableCollection<Alapanyag> AlapanyagLista => HozzavalokVM.AlapanyagLista;

        public string UjAlapanyagNev
        {
            get => HozzavalokVM.UjAlapanyagNev;
            set => HozzavalokVM.UjAlapanyagNev = value;
        }
        public string UjMertekegyseg
        {
            get => HozzavalokVM.UjMertekegyseg;
            set => HozzavalokVM.UjMertekegyseg = value;
        }

        public RelayCommand AddAlapanyagCommand => HozzavalokVM.AddAlapanyagCommand;

        public void LoadIngredients() => HozzavalokVM.LoadIngredients();
        #endregion

        #region === Pass-through: Hűtő & Spájz fül ===
        public ObservableCollection<Raktar> RaktarLista => RaktarVM.RaktarLista;

        public Alapanyag? SelectedAlapanyag
        {
            get => RaktarVM.SelectedAlapanyag;
            set => RaktarVM.SelectedAlapanyag = value;
        }

        public string UjMennyiseg
        {
            get => RaktarVM.UjMennyiseg;
            set => RaktarVM.UjMennyiseg = value;
        }

        public RelayCommand AddToRaktarCommand => RaktarVM.AddToRaktarCommand;

        public void LoadStock() => RaktarVM.LoadStock();
        #endregion


        #region === Helper ===
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void RebuildShoppingList()
        {
            ShoppingList.Clear();

            // 1) Kiválasztott receptek nevei (ReceptVM gyűjteményére támaszkodunk)
            var selected = new HashSet<string>(ReceptVM.SelectedRecipes, StringComparer.OrdinalIgnoreCase);
            if (selected.Count == 0) return;

            // 2) Adatok betöltése a szolgáltatásból
            List<Recept> allRecipes;
            List<Raktar> stock;
            try
            {
                allRecipes = _service.ListazRecepteket();
                stock = _service.ListazRaktart();
            }
            catch
            {
                return;
            }

            // 3) Szükséges mennyiségek összegzése alapanyagonként
            var need = new Dictionary<int, (string Nev, string Mertek, decimal Ossz)>();
            foreach (var rec in allRecipes)
            {
                if (!selected.Contains(rec.ReceptNev)) continue;
                foreach (var h in rec.Hozzavalok)
                {
                    var id = h.AlapanyagID;
                    var nev = h.Alapanyag.AlapanyagNev;
                    var mer = h.Alapanyag.Mertekegyseg;
                    var qty = h.SzükségesMennyiseg;

                    if (need.TryGetValue(id, out var agg))
                        need[id] = (nev, mer, agg.Ossz + qty);
                    else
                        need[id] = (nev, mer, qty);
                }
            }

            if (need.Count == 0) return;

            // 4) Készlet leképezése és hiány számítása
            var stockById = stock.ToDictionary(r => r.AlapanyagID, r => r.Mennyiseg);
            var rows = need
                .Select(kvp =>
                {
                    var id = kvp.Key;
                    var (nev, mer, ossz) = kvp.Value;
                    var have = stockById.TryGetValue(id, out var s) ? s : 0m;
                    var missing = ossz - have;
                    return (nev, mer, missing);
                })
                .Where(t => t.missing > 0m)
                .OrderBy(t => t.nev)
                .ToList();

            // 5) ShoppingList feltöltése a nézet felé (MainWindow.ShoppingRow DTO)
            foreach (var (nev, mer, missing) in rows)
                ShoppingList.Add(new ShoppingRow { Alapanyag = nev, HianyMennyiseg = missing, Mertekegyseg = mer });
        }

        #endregion
    }

    public class RecipeInfo
    {
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
