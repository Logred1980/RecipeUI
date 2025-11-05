using System.ComponentModel;

namespace RecipeUI.Models
{
    public class AddRecipeRow : INotifyPropertyChanged
    {
        private int _alapanyagId;
        private string _mennyiseg = string.Empty;
        private string _mertekegyseg = string.Empty;

        public int AlapanyagID
        {
            get => _alapanyagId;
            set { _alapanyagId = value; OnPropertyChanged(nameof(AlapanyagID)); }
        }

        public string Mennyiseg
        {
            get => _mennyiseg;
            set { _mennyiseg = value; OnPropertyChanged(nameof(Mennyiseg)); }
        }

        public string Mertekegyseg
        {
            get => _mertekegyseg;
            set { _mertekegyseg = value; OnPropertyChanged(nameof(Mertekegyseg)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
