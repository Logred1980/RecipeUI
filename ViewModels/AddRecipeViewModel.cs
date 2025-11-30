using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input; // CommandManager
using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;

namespace RecipeUI.ViewModels
{
    public class AddRecipeViewModel : INotifyPropertyChanged
    {
        #region === Mezők és események ===

        private readonly IReceptSzerviz _service;

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? CloseRequested;

        #endregion

        #region === Parancsok (RelayCommand-ok) ===

        public RelayCommand CloseCommand { get; }
        public RelayCommand SaveCommand { get; }

        #endregion

        #region === Propertyk ===

        private string _receptNev = string.Empty;
        /// <summary>Új recept neve.</summary>
        public string ReceptNev
        {
            get => _receptNev;
            set
            {
                _receptNev = value;
                OnPropertyChanged(nameof(ReceptNev));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _leiras = string.Empty;
        /// <summary>Új recept elkészítési leírása.</summary>
        public string Leiras
        {
            get => _leiras;
            set { _leiras = value; OnPropertyChanged(nameof(Leiras)); }
        }

        #endregion

        #region === Gyűjtemények (UI bindinghoz) ===

        /// <summary>Recept hozzávaló sorok (DataGrid forrása).</summary>
        public ObservableCollection<AddRecipeRow> Sorok { get; } = new();

        /// <summary>Választható alapanyagok listája (ComboBox forrása).</summary>
        public ObservableCollection<Alapanyag> Alapanyagok { get; } = new();

        #endregion

        #region === Konstruktor ===

        public AddRecipeViewModel(IReceptSzerviz service)
        {
            _service = service;

            Sorok.Add(new AddRecipeRow());

            Alapanyagok.Clear();
            foreach (var item in _service.ListazAlapanyagok())
                Alapanyagok.Add(item);

            Sorok.CollectionChanged += Sorok_CollectionChanged;
            foreach (var r in Sorok)
                HookRow(r);

            CloseCommand = new RelayCommand(_ => OnCloseRequested());
            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        }

        #endregion

        #region === Sor-szinkronizálás (AlapanyagID → Mertekegyseg) ===

        private void Sorok_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (AddRecipeRow r in e.NewItems)
                    HookRow(r);

            if (e.OldItems != null)
                foreach (AddRecipeRow r in e.OldItems)
                    UnhookRow(r);

            CommandManager.InvalidateRequerySuggested();
        }

        private void HookRow(AddRecipeRow row)
        {
            row.PropertyChanged += Row_PropertyChanged;
            if (row.AlapanyagID != 0)
                UpdateMertekegyseg(row);
        }

        private void UnhookRow(AddRecipeRow row)
        {
            row.PropertyChanged -= Row_PropertyChanged;
        }

        private void Row_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is AddRecipeRow row)
            {
                if (e.PropertyName == nameof(AddRecipeRow.AlapanyagID))
                    UpdateMertekegyseg(row);

                if (e.PropertyName == nameof(AddRecipeRow.AlapanyagID) ||
                    e.PropertyName == nameof(AddRecipeRow.Mennyiseg))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private void UpdateMertekegyseg(AddRecipeRow row)
        {
            var sel = Alapanyagok.FirstOrDefault(a => a.AlapanyagID == row.AlapanyagID);
            row.Mertekegyseg = sel?.Mertekegyseg ?? string.Empty;
        }

        #endregion

        #region === Mentés (SaveCommand) ===

        private static bool TryGetPositiveQuantity(string? text, out decimal qty)
        {
            if (decimal.TryParse((text ?? string.Empty).Trim(), out qty))
                return qty > 0m;
            qty = 0m;
            return false;
        }

        private bool CanSave()
        {
            if (string.IsNullOrWhiteSpace(ReceptNev))
                return false;

            return Sorok.Any(r => r.AlapanyagID > 0 && TryGetPositiveQuantity(r.Mennyiseg, out _));
        }

        private void Save()
        {
            var nev = (ReceptNev ?? string.Empty).Trim();
            var leiras = (Leiras ?? string.Empty).Trim();

            var hozzavalok = new Dictionary<int, decimal>();

            foreach (var r in Sorok)
            {
                if (r.AlapanyagID <= 0) continue;
                if (!TryGetPositiveQuantity(r.Mennyiseg, out var q)) continue;

                if (hozzavalok.ContainsKey(r.AlapanyagID))
                    hozzavalok[r.AlapanyagID] += q;
                else
                    hozzavalok[r.AlapanyagID] = q;
            }

            if (hozzavalok.Count == 0)
            {
                MessageBox.Show("Adj meg legalább egy hozzávalót mennyiséggel.", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ok = _service.HozzaadRecept(nev, leiras, hozzavalok);
                if (!ok)
                {
                    MessageBox.Show("Már létezik ilyen nevű recept.", "Nem sikerült",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                OnCloseRequested();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        private void OnCloseRequested()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
