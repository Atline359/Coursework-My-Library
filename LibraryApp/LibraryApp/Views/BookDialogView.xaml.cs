using System;
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
            var book = DataContext as Book;
            
            // Проверка: год не может быть выше текущего
            int currentYear = DateTime.Now.Year;
            if (book.Year > currentYear)
            {
                MessageBox.Show($"Год выпуска не может быть больше {currentYear}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Проверка: название не пустое
            if (string.IsNullOrWhiteSpace(book.Title))
            {
                MessageBox.Show("Название книги не может быть пустым", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Проверка: автор не пустой
            if (string.IsNullOrWhiteSpace(book.Author))
            {
                MessageBox.Show("Автор не может быть пустым", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Проверка: страниц больше 0
            if (book.Pages <= 0)
            {
                MessageBox.Show("Количество страниц должно быть больше 0", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
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