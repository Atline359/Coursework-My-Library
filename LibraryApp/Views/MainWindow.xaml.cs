using System.Windows;
using LibraryApp.ViewModels;

namespace LibraryApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}