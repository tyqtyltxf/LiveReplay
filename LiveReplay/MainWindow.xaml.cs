using System.Windows;
using System.Windows.Controls;
using LiveReplay.ViewModels;

namespace LiveReplay
{
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _viewModel.NavigateRequested += OnNavigateRequested;

            MainFrame.Navigate(new Views.ReplayPage());
        }

        private void OnNavigateRequested(Page page)
        {
            MainFrame.Navigate(page);
        }
    }
}