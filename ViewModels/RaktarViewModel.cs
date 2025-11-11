using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RecipeUI.ViewModels
{
    public class RaktarViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<Raktar> RaktarLista { get; } = new();

        private Alapanyag? _selectedAlapanyag;
        public Alapanyag? SelectedAlapanyag
        {
            get => _selectedAlapanyag;
            set { _selectedAlapanyag = value; OnPropertyChanged(nameof(SelectedAlapanyag)); }
        }

        private string _ujMennyiseg = string.Empty;
        public string UjMennyiseg
        {
            get => _ujMennyiseg;
            set { _ujMennyiseg = value; OnPropertyChanged(nameof(UjMennyiseg)); }
        }

        public RelayCommand AddToRaktarCommand { get; }

        public RaktarViewModel(IReceptSzerviz service)
        {
            _service = service;
            AddToRaktarCommand = new RelayCommand(_ => AddToStock());
        }

        public void LoadStock()
        {
            RaktarLista.Clear();
            foreach (var r in _service.ListazRaktart().Where(x => x.Mennyiseg > 0))
                RaktarLista.Add(r);
        }

        private void AddToStock()
        {
            if (SelectedAlapanyag == null) return;
            if (!decimal.TryParse(UjMennyiseg, out var qty) || qty <= 0) return;

            try
            {
                _service.HozzaadRaktarhoz(SelectedAlapanyag.AlapanyagID, qty);
                UjMennyiseg = string.Empty;
                LoadStock();
            }
            catch
            {
                // később: felhasználóbarát hibaüzenet
            }
        }

        private void OnPropertyChanged(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
