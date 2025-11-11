using RecipeUI.Data;
using RecipeUI.Services;
using RecipeUI.ViewModels;
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

        private bool _isDarkTheme = true;

        #region === Konstruktor és inicializálás ===

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
            BtnThemeToggle.Click += BtnThemeToggle_Click;

            SetTheme(isDark: false);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _vm.LoadRecipes();
            _vm.LoadIngredients();
            _vm.LoadStock();
        }

        #endregion

        #region === Tab váltás ===

        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource is not TabControl) return;

            if (tabHozzavalok.IsSelected)
                _vm.LoadIngredients();

            else if (tabRaktar.IsSelected)
                _vm.LoadStock();
        }

        #endregion

        #region === Felület színváltás / téma ===

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

        #region === Recept panel megjelenítés / elrejtés ===

        private void OnRecipeAddRequested(object? sender, EventArgs e)
        {
            if (AddRecipePanel.Visibility == Visibility.Visible)
            {
                AddRecipePanel.Visibility = Visibility.Collapsed;
                return;
            }

            AddRecipeVM = new AddRecipeViewModel(_receptSzerviz);
            AddRecipeVM.CloseRequested += OnAddRecipeCloseRequested;
            AddRecipePanel.DataContext = AddRecipeVM;
            AddRecipePanel.Visibility = Visibility.Visible;
            TxtUjReceptNev.Focus();
        }

        private void OnAddRecipeCloseRequested(object? sender, EventArgs e)
        {
            AddRecipePanel.Visibility = Visibility.Collapsed;
            _vm.LoadRecipes();
        }

        #endregion

        #region === DTO-k ===

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
