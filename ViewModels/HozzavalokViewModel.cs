using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace RecipeUI.ViewModels
{
    public class HozzavalokViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Alapanyag> AlapanyagLista { get; } = new();

        private string _ujAlapanyagNev = string.Empty;
        public string UjAlapanyagNev
        {
            get => _ujAlapanyagNev;
            set { _ujAlapanyagNev = value; OnPropertyChanged(nameof(UjAlapanyagNev)); }
        }

        private string _ujMertekegyseg = string.Empty;
        public string UjMertekegyseg
        {
            get => _ujMertekegyseg;
            set { _ujMertekegyseg = value; OnPropertyChanged(nameof(UjMertekegyseg)); }
        }

        public RelayCommand AddAlapanyagCommand { get; }

        public HozzavalokViewModel(IReceptSzerviz service)
        {
            _service = service;
            AddAlapanyagCommand = new RelayCommand(_ => AddNewIngredient());
        }

        public void LoadIngredients()
        {
            AlapanyagLista.Clear();
            foreach (var a in _service.ListazAlapanyagok())
                AlapanyagLista.Add(a);
        }

        private void AddNewIngredient()
        {
            var nev = string.Join(" ", (UjAlapanyagNev ?? string.Empty).Trim()
                                           .Split(' ', StringSplitOptions.RemoveEmptyEntries))
                      .ToLowerInvariant();
            var mertek = (UjMertekegyseg ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(nev))
            {
                MessageBox.Show("Add meg az alapanyag nevét!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(mertek))
            {
                MessageBox.Show("Add meg a mértékegységet!", "Hiányzó adat",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var ok = _service.HozzaadAlapanyag(nev, mertek);
                if (!ok)
                {
                    MessageBox.Show("Már létezik ilyen nevű alapanyag.", "Nem sikerült",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                UjAlapanyagNev = string.Empty;
                UjMertekegyseg = string.Empty;

                LoadIngredients();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Mentési hiba: {ex.Message}", "Hiba",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
