using System.Windows;
using LibraryApp.Models;

namespace LibraryApp.Views
{
    public partial class BookDialogView : Window
    {
        public BookDialogView(Book book)
        {
            InitializeComponent();
            DataContext = book;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}