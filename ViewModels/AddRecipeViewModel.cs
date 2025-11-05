using RecipeUI.Commands;
using RecipeUI.Models;
using RecipeUI.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RecipeUI.ViewModels
{
    public class AddRecipeViewModel : INotifyPropertyChanged
    {
        private readonly IReceptSzerviz _service;

        public RelayCommand CloseCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler? CloseRequested;

        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _receptNev = string.Empty;
        public string ReceptNev
        {
            get => _receptNev;
            set { _receptNev = value; OnPropertyChanged(nameof(ReceptNev)); }
        }

        private string _leiras = string.Empty;
        public string Leiras
        {
            get => _leiras;
            set { _leiras = value; OnPropertyChanged(nameof(Leiras)); }
        }

        public ObservableCollection<AddRecipeRow> Sorok { get; } = new();
        public ObservableCollection<Alapanyag> Alapanyagok { get; } = new();

        public AddRecipeViewModel(IReceptSzerviz service)
        {
            _service = service;

            Sorok.Add(new AddRecipeRow());

            Alapanyagok.Clear();
            foreach (var item in _service.ListazAlapanyagok())
                Alapanyagok.Add(item);

            CloseCommand = new RelayCommand(_ => OnCloseRequested());
        }
        private void OnCloseRequested()
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

    }
}
