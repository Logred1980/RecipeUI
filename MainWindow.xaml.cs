using Microsoft.EntityFrameworkCore;
using RecipeUI.Data;
using RecipeUI.Models;
using RecipeUI.Services;
using RecipeUI.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;


namespace RecipeUI
{
    public partial class MainWindow : Window
    {
        public AddRecipeViewModel AddRecipeVM { get; private set; }

        private readonly MainViewModel _vm;

        private readonly ReceptSzerviz _receptSzerviz;

        private bool _isDarkTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            _receptSzerviz = new ReceptSzerviz(new RecipeDbContext());
            _vm = new MainViewModel(_receptSzerviz);
            DataContext = _vm;

            AddRecipeVM = new AddRecipeViewModel(_receptSzerviz);
            AddRecipeVM.CloseRequested += OnAddRecipeCloseRequested;

            AddRecipePanel.DataContext = AddRecipeVM;

            _vm.RecipeAddRequested += OnRecipeAddRequested;

            Loaded += MainWindow_Loaded;

            MainTabs.SelectionChanged += MainTabs_SelectionChanged;
            
            BtnThemeToggle.Click += BtnThemeToggle_Click;
            BtnBeszerezve.Click += BtnBeszerezve_Click;
            BtnElkeszitve.Click += BtnElkeszitve_Click;

            SetTheme(isDark: false);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.LoadRecipes();
        }

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is not TabControl) return;

            if (tabHozzavalok.IsSelected)
            {
                RefreshIngredientsGrid();
            }
            else if (tabRaktar.IsSelected)
            {
                LoadAlapanyagCombo();
                RefreshStockGrid();
            }
        }

        private static bool TryParseDecimal(string input, out decimal value)
        {
            var styles = System.Globalization.NumberStyles.Number;
            var cc = System.Globalization.CultureInfo.CurrentCulture;
            if (decimal.TryParse(input, styles, cc, out value)) return true;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return decimal.TryParse(input, styles, inv, out value);
        }

        #region Felület színváltás

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            SetTheme(!_isDarkTheme);
        }

        private void SetTheme(bool isDark)
        {
            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            if (isDark)
            {
                dicts.Add(new ResourceDictionary { Source = new Uri("Themes/Dark.xaml", UriKind.Relative) });
                ImgTheme.Source = new BitmapImage(new Uri("Assets/sun.png", UriKind.Relative));
            }
            else
            {
                dicts.Add(new ResourceDictionary { Source = new Uri("Themes/Light.xaml", UriKind.Relative) });
                ImgTheme.Source = new BitmapImage(new Uri("Assets/moon.png", UriKind.Relative));
            }

            _isDarkTheme = isDark;
        }

        #endregion


        #region TAB: Hűtő & Spájz

        private void LoadAlapanyagCombo()
        {
            try
            {
                var alapanyagok = _receptSzerviz.ListazAlapanyagok();
                CmbAlapanyag.ItemsSource = alapanyagok;
                CmbAlapanyag.Items.Refresh();
                if (CmbAlapanyag.SelectedIndex < 0 && alapanyagok.Count > 0)
                    CmbAlapanyag.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az alapanyagok betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStockGrid()
        {
            try
            {
                var list = _receptSzerviz.ListazRaktart()
                                 .Where(r => r.Mennyiseg > 0m)
                                 .OrderBy(r => r.Alapanyag.AlapanyagNev)
                                 .ToList();

                RefrigeratorStock.ItemsSource = list;
                RefrigeratorStock.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a készlet listázásakor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region TAB: Hozzávalók

        private void RefreshIngredientsGrid()
        {
            try
            {
                var list = _receptSzerviz.ListazAlapanyagok();
                ItemDB.ItemsSource = list;
                ItemDB.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az alapanyagok listázásakor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Gombok

        private void BtnBeszerezve_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedRecipes.Count == 0)
            {
                MessageBox.Show("A jobb oldali listában nincs egy recept sem. Előbb adj hozzá recepteket!", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<Recept> allRecipes;
            List<Raktar> stock;

            try
            {
                allRecipes = _receptSzerviz.ListazRecepteket();
                stock = _receptSzerviz.ListazRaktart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az adatok betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSet = new HashSet<string>(_vm.SelectedRecipes, StringComparer.OrdinalIgnoreCase);

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
                if (missing > 0m)
                    toAdd.Add((id, missing));
            }

            if (toAdd.Count == 0)
            {
                MessageBox.Show("Minden hozzávaló megvan. Nincs mit hozzáadni a raktárhoz.", "Kész",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                _vm.SelectedRecipes.Clear();
                return;
            }

            try
            {
                foreach (var (id, qty) in toAdd)
                    _receptSzerviz.HozzaadRaktarhoz(id, qty);

                RefreshStockGrid();

                _vm.SelectedRecipes.Clear();

                MessageBox.Show("A bevásárlólista tételeit hozzáadtuk a Hűtő & Spájz készlethez.", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba a raktár frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnElkeszitve_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.SelectedRecipes.Count == 0)
            {
                MessageBox.Show("A jobb oldali listában nincs egy recept sem. Előbb adj hozzá recepteket!", "Nincs adat",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            List<Recept> allRecipes;
            try
            {
                allRecipes = _receptSzerviz.ListazRecepteket();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a receptek betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedSet = new HashSet<string>(_vm.SelectedRecipes, StringComparer.OrdinalIgnoreCase);

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
                stockList = _receptSzerviz.ListazRaktart();
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
                    {
                        _receptSzerviz.HozzaadRaktarhoz(alapanyagId, -consume);
                    }
                }

                RefreshStockGrid();

                _vm.SelectedRecipes.Clear();

                MessageBox.Show("A kiválasztott receptek hozzávalóit levontuk a Hűtő & Spájz készletből (negatív készlet nélkül).", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a raktár frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddItem_DB_Click(object sender, RoutedEventArgs e)
        {
            var nev = (TxtAlapanyagNev.Text ?? string.Empty).Trim();
            var mertek = (TxtMertekegyseg.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nev))
            {
                MessageBox.Show("Add meg az alapanyag nevét!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtAlapanyagNev.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(mertek))
            {
                MessageBox.Show("Add meg a mértékegységet!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMertekegyseg.Focus();
                return;
            }

            try
            {
                var ok = _receptSzerviz.HozzaadAlapanyag(nev, mertek);
                if (!ok)
                {
                    MessageBox.Show("Már létezik ilyen nevű alapanyag.", "Nem sikerült",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtAlapanyagNev.Clear();
                TxtMertekegyseg.Clear();
                TxtAlapanyagNev.Focus();

                RefreshIngredientsGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddItem_RS_Click(object sender, RoutedEventArgs e)
        {
            if (CmbAlapanyag.SelectedValue == null)
            {
                MessageBox.Show("Válassz alapanyagot!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                CmbAlapanyag.Focus();
                return;
            }

            var txt = (TxtMennyiseg.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(txt))
            {
                MessageBox.Show("Add meg a mennyiséget!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMennyiseg.Focus();
                return;
            }

            if (!TryParseDecimal(txt, out var qty) || qty <= 0)
            {
                MessageBox.Show("Érvénytelen mennyiség.", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtMennyiseg.SelectAll();
                TxtMennyiseg.Focus();
                return;
            }

            try
            {
                int alapanyagId = (int)CmbAlapanyag.SelectedValue;
                _receptSzerviz.HozzaadRaktarhoz(alapanyagId, qty);

                TxtMennyiseg.Clear();
                RefreshStockGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnRecipeAddRequested(object? sender, EventArgs e)
        {
            // Ha látható → csukjuk össze
            if (AddRecipePanel.Visibility == Visibility.Visible)
            {
                AddRecipePanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Ha rejtve volt → nyissuk meg és készítsünk új viewmodelt
            AddRecipeVM = new AddRecipeViewModel(_receptSzerviz);
            AddRecipePanel.DataContext = AddRecipeVM;
            AddRecipePanel.Visibility = Visibility.Visible;
            TxtUjReceptNev.Focus();
        }


        private void OnAddRecipeCloseRequested(object? sender, EventArgs e)
        {
            AddRecipePanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region DTO-k

        public class ReceptReszletRow
        {
            public string Alapanyag { get; set; } = string.Empty;
            public decimal Mennyiseg { get; set; }
            public string Mertekegyseg { get; set; } = string.Empty;
        }

        public class ShoppingRow
        {
            public string Alapanyag { get; set; } = string.Empty;
            public decimal HianyMennyiseg { get; set; }
            public string Mertekegyseg { get; set; } = string.Empty;
        }

        #endregion
    }
}
