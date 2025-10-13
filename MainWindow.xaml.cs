using Microsoft.EntityFrameworkCore;
using RecipeUI.Data;
using RecipeUI.Models;
using RecipeUI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RecipeUI
{
    public partial class MainWindow : Window
    {
        private readonly ReceptSzerviz _receptSzerviz;
        private CancellationTokenSource? _reloadCts;

        // Lenyíló szerkesztő állapot
        private readonly ObservableCollection<AddRecipeRow> _addRecipeRows = new();
        private List<Alapanyag> _allIngredients = new();

        // --- ÚJ: Kijelölt receptek neveinek gyűjteménye a jobb oldali listához ---
        private readonly ObservableCollection<string> _selectedRecipes = new();

        #region Általános

        // Alap konstruktor: init, eseményfeliratkozások
        public MainWindow()
        {
            InitializeComponent();
            _receptSzerviz = new ReceptSzerviz(new RecipeDbContext());

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
            MainTabs.SelectionChanged += MainTabs_SelectionChanged;
            RecipeList.SelectionChanged += RecipeList_SelectionChanged;

            BtnUjRecept.Click += BtnUjRecept_Click;
            BtnUjReceptMegsem.Click += BtnUjReceptMegsem_Click;
            BtnUjReceptMentes.Click += BtnUjReceptMentes_Click;
            AddRecipeGrid.CurrentCellChanged += (_, __) => SyncRowUnits();
            AddRecipeGrid.ItemsSource = _addRecipeRows;

            AddRecipeGrid.ItemsSource = _addRecipeRows;

            BtnHozzaad.Click += BtnHozzaad_Click;
            SelectedRecipesList.ItemsSource = _selectedRecipes;

            BtnListaKeszit.Click += BtnListaKeszit_Click;

            BtnBeszerezve.Click += BtnBeszerezve_Click;

            BtnElkeszitve.Click += BtnElkeszitve_Click;
        }

        // Ablak betöltésekor - receptlista biztonságos újratöltése
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await SafeReloadAsync();
        }

        // Ablak bezárásakor - futó újratöltések leállítása
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _reloadCts?.Cancel();
            _reloadCts?.Dispose();
            _reloadCts = null;
        }

        // Tab váltás kezelése
        private async void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is not TabControl) return;

            if (tabRecept.IsSelected)
            {
                await SafeReloadAsync();
            }
            else if (tabHozzavalok.IsSelected)
            {
                RefreshIngredientsGrid();
            }
            else if (tabRaktar.IsSelected)
            {
                LoadAlapanyagCombo();
                RefreshStockGrid();
            }
        }

        // Biztonságos receptlista újratöltés megszakítható tokennel
        private async Task SafeReloadAsync()
        {
            _reloadCts?.Cancel();
            _reloadCts = new CancellationTokenSource();
            var ct = _reloadCts.Token;

            try
            {
                await LoadRecipeListAsync(ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a receptek listázásakor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                RecipeList.ItemsSource = null;
            }
        }

        // Közös decimális parse (vessző és pont)
        private static bool TryParseDecimal(string input, out decimal value)
        {
            var styles = System.Globalization.NumberStyles.Number;
            var cc = System.Globalization.CultureInfo.CurrentCulture;
            if (decimal.TryParse(input, styles, cc, out value)) return true;

            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return decimal.TryParse(input, styles, inv, out value);
        }

        #endregion

        #region tabRecept

        // Receptnevek betöltése
        private async Task LoadRecipeListAsync(CancellationToken ct)
        {
            var recipeNames = await Task.Run(() =>
            {
                using var ctx = new RecipeDbContext();
                var svc = new ReceptSzerviz(ctx);

                var names = svc.ListazRecepteket()
                               .Select(r => r.ReceptNev)
                               .Where(n => !string.IsNullOrWhiteSpace(n))
                               .Distinct()
                               .OrderBy(n => n)
                               .ToList();

                return names;
            }, ct);

            if (ct.IsCancellationRequested) return;

            if (!Dispatcher.CheckAccess())
                await Dispatcher.InvokeAsync(() => UpdateRecipeListUI(recipeNames));
            else
                UpdateRecipeListUI(recipeNames);
        }

        // Bal oldali receptlista összeállítása
        private void UpdateRecipeListUI(List<string> recipeNames)
        {
            recipeNames ??= new List<string>();

            if (CommonDatas.RecipeNameList == null)
                CommonDatas.RecipeNameList = new List<string>();
            else
                CommonDatas.RecipeNameList.Clear();

            CommonDatas.RecipeNameList.AddRange(recipeNames);

            GenerateRecipeIDs();

            var recipes = new List<RecipeInfo>();
            for (int i = 0; i < CommonDatas.RecipeNameList.Count; i++)
            {
                var name = CommonDatas.RecipeNameList[i];
                var id = (CommonDatas.RecipeIDList != null && i < CommonDatas.RecipeIDList.Count)
                    ? CommonDatas.RecipeIDList[i]
                    : string.Empty;

                recipes.Add(new RecipeInfo
                {
                    ID = id,
                    Name = name
                });
            }

            RecipeList.ItemsSource = recipes;
        }

        // Recepthez rövid azonosítók generálása
        private void GenerateRecipeIDs()
        {
            if (CommonDatas.RecipeIDList == null)
                CommonDatas.RecipeIDList = new List<string>();
            else
                CommonDatas.RecipeIDList.Clear();

            if (CommonDatas.RecipeNameList == null || CommonDatas.RecipeNameList.Count == 0)
                return;

            string temporaryID = "";
            int helperSerialNr = 0;

            foreach (var item in CommonDatas.RecipeNameList)
            {
                string[] words = item.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length > 1)
                {
                    temporaryID = $"{char.ToUpper(words[0][0])}{char.ToUpper(words[1][0])}";
                }
                else
                {
                    temporaryID = $"{char.ToUpper(words[0][0])}{char.ToUpper(words[0][words[0].Length - 1])}";
                }

                if (CommonDatas.RecipeIDList.Contains(temporaryID))
                {
                    helperSerialNr++;
                    temporaryID += helperSerialNr.ToString();
                }

                CommonDatas.RecipeIDList.Add(temporaryID);
            }
        }

        // Bal oldalon recept kiválasztása
        private void RecipeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RecipeList.SelectedItem is RecipeInfo info)
            {
                ShowSelectedRecipe(info.Name);
            }
            else
            {
                ClearRecipeView();
            }
        }

        // Kijelölt recept betöltése és megjelenítése a középső panelen
        private void ShowSelectedRecipe(string recipeName)
        {
            try
            {
                var recept = _receptSzerviz.ListazRecepteket()
                                           .FirstOrDefault(r => r.ReceptNev == recipeName);

                if (recept == null)
                {
                    ClearRecipeView();
                    return;
                }

                SetRecipeText(recept.ReceptNev, recept.Elkeszites);

                var rows = recept.Hozzavalok
                    .OrderBy(h => h.Alapanyag.AlapanyagNev)
                    .Select(h => new ReceptReszletRow
                    {
                        Alapanyag = h.Alapanyag.AlapanyagNev,
                        Mennyiseg = h.SzükségesMennyiseg,
                        Mertekegyseg = h.Alapanyag.Mertekegyseg
                    })
                    .ToList();

                RecipeGrideView.ItemsSource = rows;
                RecipeGrideView.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba a recept betöltésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ClearRecipeView();
            }
        }

        // Kijelölt recept megjelenítő elemek ürítése
        private void ClearRecipeView()
        {
            RecipeText.Inlines.Clear();
            RecipeGrideView.ItemsSource = null;
            RecipeGrideView.Items.Refresh();
        }

        // Felső szövegmező (név félkövér + új sor + leírás) beállítása
        private void SetRecipeText(string name, string description)
        {
            RecipeText.Inlines.Clear();
            RecipeText.Inlines.Add(new System.Windows.Documents.Run(name) { FontWeight = FontWeights.Bold });
            RecipeText.Inlines.Add(new System.Windows.Documents.LineBreak());
            if (!string.IsNullOrWhiteSpace(description))
                RecipeText.Inlines.Add(new System.Windows.Documents.Run(description));
        }

        // Recept hozzáadás panel nyitása/zárása a jobb felső gombbal
        private void BtnUjRecept_Click(object? sender, RoutedEventArgs e)
        {
            if (AddRecipePanel.Visibility != Visibility.Visible)
                OpenAddRecipeEditor();
            else
                AddRecipePanel.Visibility = Visibility.Collapsed;
        }

        // Recept hozzáadás panel előkészítése és megnyitása
        private void OpenAddRecipeEditor()
        {
            try
            {
                _allIngredients = _receptSzerviz.ListazAlapanyagok();

                if (AddRecipe_AlapanyagColumn != null)
                    AddRecipe_AlapanyagColumn.ItemsSource = _allIngredients;

                TxtUjReceptNev.Clear();
                TxtUjReceptLeiras.Clear();
                _addRecipeRows.Clear();
                _addRecipeRows.Add(new AddRecipeRow()); // egy üres sor kezdésnek

                AddRecipePanel.Visibility = Visibility.Visible;
                TxtUjReceptNev.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az editor megnyitásakor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Recept hozzáadás panel bezárása mentés nélkül
        private void BtnUjReceptMegsem_Click(object? sender, RoutedEventArgs e)
        {
            AddRecipePanel.Visibility = Visibility.Collapsed;
        }

        // Új recept mentése a lenyíló szerkesztőből
        private void BtnUjReceptMentes_Click(object? sender, RoutedEventArgs e)
        {
            var nev = (TxtUjReceptNev.Text ?? string.Empty).Trim();
            var leiras = (TxtUjReceptLeiras.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nev))
            {
                MessageBox.Show("Add meg a recept nevét!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUjReceptNev.Focus();
                return;
            }

            SyncRowUnits();

            var dict = new Dictionary<int, decimal>();
            foreach (var row in _addRecipeRows)
            {
                if (row.AlapanyagID <= 0) continue;
                if (string.IsNullOrWhiteSpace(row.Mennyiseg)) continue;

                if (!TryParseDecimal(row.Mennyiseg.Trim(), out var qty) || qty <= 0)
                {
                    MessageBox.Show("Érvénytelen mennyiség valamelyik hozzávalónál.", "Hiba",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dict.ContainsKey(row.AlapanyagID)) dict[row.AlapanyagID] += qty;
                else dict[row.AlapanyagID] = qty;
            }

            if (dict.Count == 0)
            {
                MessageBox.Show("Adj meg legalább egy érvényes hozzávalót!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ok = _receptSzerviz.HozzaadRecept(nev, leiras, dict);
                if (!ok)
                {
                    MessageBox.Show("Már létezik ilyen nevű recept.", "Nem sikerült",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // siker → panel zár, mezők tisztítás, bal lista frissítés
                AddRecipePanel.Visibility = Visibility.Collapsed;
                _addRecipeRows.Clear();
                TxtUjReceptNev.Clear();
                TxtUjReceptLeiras.Clear();

                _ = SafeReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Lenyíló rács soraiban a mértékegységek szinkronizálása az alapanyag alapján
        private void SyncRowUnits()
        {
            if (_allIngredients == null || _allIngredients.Count == 0) return;

            foreach (var row in _addRecipeRows)
            {
                if (row.AlapanyagID <= 0)
                {
                    row.Mertekegyseg = string.Empty;
                    continue;
                }

                var ing = _allIngredients.FirstOrDefault(a => a.AlapanyagID == row.AlapanyagID);
                row.Mertekegyseg = ing?.Mertekegyseg ?? string.Empty;
            }
        }

        // --- ÚJ: "Hozzáadás a listához" gomb – bal oldali kijelölt recept nevét hozzáadja a jobb oldali listához ---
        private void BtnHozzaad_Click(object sender, RoutedEventArgs e)
        {
            if (RecipeList.SelectedItem is not RecipeInfo info)
            {
                MessageBox.Show("Válassz ki egy receptet a listából!", "Nincs kiválasztás",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var name = (info.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            if (!_selectedRecipes.Contains(name))
            {
                _selectedRecipes.Add(name);
            }
        }

        // „Lista készítése” gomb – a jobb oldali listában lévő receptek összesített hiányzó hozzávalóit számolja és megjeleníti
        private void BtnListaKeszit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipes.Count == 0)
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

            var selectedSet = new HashSet<string>(_selectedRecipes, StringComparer.OrdinalIgnoreCase);

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

            var shopping = new List<ShoppingRow>();
            foreach (var kvp in requiredByIngredient)
            {
                var id = kvp.Key;
                var (nev, mertek, need) = kvp.Value;
                var have = stockByIngredient.TryGetValue(id, out var s) ? s : 0m;
                var missing = need - have;
                if (missing > 0)
                {
                    shopping.Add(new ShoppingRow
                    {
                        Alapanyag = nev,
                        HianyMennyiseg = missing,
                        Mertekegyseg = mertek
                    });
                }
            }

            var ordered = shopping.OrderBy(x => x.Alapanyag).ToList();
            ShoppingGrid.ItemsSource = ordered.Count > 0 ? ordered : null;
            ShoppingGrid.Items.Refresh();

            if (ordered.Count == 0)
            {
                MessageBox.Show("Minden hozzávaló megvan a Hűtő & Spájz készletben. Nincs hiány.", "Kész 🎉",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // „Beszerezve → Hűtő” gomb – a jobb oldali listában lévő receptek alapján számolt hiányt hozzáadja a raktárhoz
        private void BtnBeszerezve_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipes.Count == 0)
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

            var selectedSet = new HashSet<string>(_selectedRecipes, StringComparer.OrdinalIgnoreCase);

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

                _selectedRecipes.Clear();
                SelectedRecipesList.Items.Refresh();
                ShoppingGrid.ItemsSource = null;
                ShoppingGrid.Items.Refresh();
                return;
            }

            try
            {
                foreach (var (id, qty) in toAdd)
                    _receptSzerviz.HozzaadRaktarhoz(id, qty);

                RefreshStockGrid();

                _selectedRecipes.Clear();
                SelectedRecipesList.Items.Refresh();

                ShoppingGrid.ItemsSource = null;
                ShoppingGrid.Items.Refresh();

                MessageBox.Show("A bevásárlólista tételeit hozzáadtuk a Hűtő & Spájz készlethez.", "Siker",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba a raktár frissítésekor: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // „Elkészítve → Levonás” gomb – a jobb oldali listában lévő receptek hozzávalóit levonja a raktárból (negatív készlet nélkül)
        private void BtnElkeszitve_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRecipes.Count == 0)
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

            var selectedSet = new HashSet<string>(_selectedRecipes, StringComparer.OrdinalIgnoreCase);

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

            // Készlet beolvasása egyszer, és szótárba rendezve
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

                    // Ennyit lehet ÉRDEMBEN levonni: a kisebbiket a szükséges és a meglévő közül
                    var consume = Math.Min(osszSzukseges, have);

                    if (consume > 0m)
                    {
                        _receptSzerviz.HozzaadRaktarhoz(alapanyagId, -consume);
                    }
                    // Ha consume == 0 → nincs mit levonni, és NEM engedünk mínuszba
                }

                RefreshStockGrid();

                _selectedRecipes.Clear();
                SelectedRecipesList.Items.Refresh();

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
        #region tabHozzavalok

        // Hozzávalók rács frissítése
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

        // Új alapanyag hozzáadása (név + mértékegység)
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

        #endregion

        #region tabRaktar

        // Raktár tab: alapanyag combo feltöltése
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

        // Raktár rács frissítése név szerinti rendezéssel
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

        // Készlethez hozzáadás a kiválasztott alapanyaggal és mennyiséggel
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

        #endregion
    }

    #region DTO-k

    public class ReceptReszletRow
    {
        public string Alapanyag { get; set; } = string.Empty;
        public decimal Mennyiseg { get; set; }
        public string Mertekegyseg { get; set; } = string.Empty;
    }

    // Lenyíló szerkesztő soraihoz
    public class AddRecipeRow : System.ComponentModel.INotifyPropertyChanged
    {
        private int _alapanyagId;
        private string _mennyiseg = string.Empty;
        private string _mertekegyseg = string.Empty;

        public int AlapanyagID
        {
            get => _alapanyagId;
            set { if (_alapanyagId != value) { _alapanyagId = value; OnPropertyChanged(nameof(AlapanyagID)); } }
        }

        public string Mennyiseg
        {
            get => _mennyiseg;
            set { if (_mennyiseg != value) { _mennyiseg = value; OnPropertyChanged(nameof(Mennyiseg)); } }
        }

        public string Mertekegyseg
        {
            get => _mertekegyseg;
            set { if (_mertekegyseg != value) { _mertekegyseg = value; OnPropertyChanged(nameof(Mertekegyseg)); } }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));
    }

    public class ShoppingRow
    {
        public string Alapanyag { get; set; } = string.Empty;
        public decimal HianyMennyiseg { get; set; }
        public string Mertekegyseg { get; set; } = string.Empty;
    }


    #endregion
}
