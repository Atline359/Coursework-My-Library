using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LibraryApp.Models;
using LibraryApp.Views;

namespace LibraryApp.ViewModels
{
    // Главная ViewModel (связь Model и View)
    public class MainViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private readonly LibraryRepository _repository;

        // Коллекции
        public ObservableCollection<Book> Books { get; set; }
        public ObservableCollection<string> Genres { get; set; }
        public ObservableCollection<string> SortOptions { get; set; }

        // Выбранная книга
        private Book? _selectedBook;
        public Book? SelectedBook
        {
            get => _selectedBook;
            set
            {
                SetProperty(ref _selectedBook, value);
                ((RelayCommand)EditBookCommand).NotifyCanExecuteChanged();
                ((RelayCommand)DeleteBookCommand).NotifyCanExecuteChanged();
                ((RelayCommand)ToggleReadCommand).NotifyCanExecuteChanged();
            }
        }

        // Фильтры
        private string _selectedGenre = "Все";
        public string SelectedGenre
        {
            get => _selectedGenre;
            set { SetProperty(ref _selectedGenre, value); ApplyFilters(); }
        }

        private bool _showOnlyUnread = false;
        public bool ShowOnlyUnread
        {
            get => _showOnlyUnread;
            set { SetProperty(ref _showOnlyUnread, value); ApplyFilters(); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); ApplyFilters(); }
        }

        private string _selectedSort = "Название (А-Я)";
        public string SelectedSort
        {
            get => _selectedSort;
            set { SetProperty(ref _selectedSort, value); ApplyFilters(); }
        }

        // Статистика
        private int _totalBooksCount;
        public int TotalBooksCount
        {
            get => _totalBooksCount;
            set => SetProperty(ref _totalBooksCount, value);
        }

        private int _readBooksCount;
        public int ReadBooksCount
        {
            get => _readBooksCount;
            set => SetProperty(ref _readBooksCount, value);
        }

        // Команды
        public ICommand AddBookCommand { get; }
        public ICommand EditBookCommand { get; }
        public ICommand DeleteBookCommand { get; }
        public ICommand ToggleReadCommand { get; }
        public ICommand ExportCsvCommand { get; }

        // Конструктор
        public MainViewModel()
        {
            _repository = new LibraryRepository();
            Books = new ObservableCollection<Book>();

            Genres = new ObservableCollection<string> { "Все", "Фантастика", "Детектив", "Наука", "Роман", "Классика" };
            SortOptions = new ObservableCollection<string> 
            { 
                "Название (А-Я)", "Название (Я-А)", "Год (новые)", "Год (старые)", "Оценка (высокая)", "Оценка (низкая)" 
            };

            AddBookCommand = new RelayCommand(AddBook);
            EditBookCommand = new RelayCommand(EditBook, () => SelectedBook != null);
            DeleteBookCommand = new RelayCommand(DeleteBook, () => SelectedBook != null);
            ToggleReadCommand = new RelayCommand(ToggleRead, () => SelectedBook != null);
            ExportCsvCommand = new RelayCommand(ExportToCsv);

            LoadBooks();
        }

        // Загрузить все книги
        private void LoadBooks()
        {
            var books = _repository.GetAll();
            Books.Clear();
            foreach (var book in books)
                Books.Add(book);
            ApplyFilters();
        }

        // Применить фильтры и сортировку
        private void ApplyFilters()
        {
            var filtered = _repository.GetAll().AsEnumerable();

            if (SelectedGenre != "Все")
                filtered = filtered.Where(b => b.Genre == SelectedGenre);

            if (ShowOnlyUnread)
                filtered = filtered.Where(b => !b.IsRead);

            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(b => 
                    b.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            filtered = SelectedSort switch
            {
                "Название (А-Я)" => filtered.OrderBy(b => b.Title),
                "Название (Я-А)" => filtered.OrderByDescending(b => b.Title),
                "Год (новые)" => filtered.OrderByDescending(b => b.Year),
                "Год (старые)" => filtered.OrderBy(b => b.Year),
                "Оценка (высокая)" => filtered.OrderByDescending(b => b.Rating),
                "Оценка (низкая)" => filtered.OrderBy(b => b.Rating),
                _ => filtered.OrderBy(b => b.Title)
            };

            Books.Clear();
            foreach (var book in filtered)
                Books.Add(book);

            UpdateStatistics();
        }

        // Обновить статистику
        private void UpdateStatistics()
        {
            var allBooks = _repository.GetAll();
            TotalBooksCount = allBooks.Count;
            ReadBooksCount = allBooks.Count(b => b.IsRead);
        }

        // Добавить книгу
        private void AddBook()
        {
            var newBook = new Book { Year = DateTime.Now.Year };
            var dialog = new BookDialogView(newBook);
            if (dialog.ShowDialog() == true)
            {
                _repository.Add(newBook);
                LoadBooks();
            }
        }

        // Редактировать книгу
        private void EditBook()
        {
            if (SelectedBook == null) return;
            var dialog = new BookDialogView(SelectedBook);
            if (dialog.ShowDialog() == true)
            {
                _repository.Update(SelectedBook);
                LoadBooks();
            }
        }

        // Удалить книгу
        private void DeleteBook()
        {
            if (SelectedBook == null) return;
            if (MessageBox.Show($"Удалить книгу \"{SelectedBook.Title}\"?", "Подтверждение", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _repository.Delete(SelectedBook.Id);
                LoadBooks();
            }
        }

        // Отметить прочитанной
        private void ToggleRead()
        {
            if (SelectedBook == null) return;
            SelectedBook.IsRead = !SelectedBook.IsRead;
            if (SelectedBook.IsRead && SelectedBook.Rating == 0)
                SelectedBook.Rating = 3;
            _repository.Update(SelectedBook);
            LoadBooks();
        }

        // Экспорт в CSV
        private void ExportToCsv()
        {
            try
            {
                var books = _repository.GetAll();
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string fileName = $"Библиотека_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                string filePath = Path.Combine(desktopPath, fileName);

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    writer.WriteLine("Id;Название;Автор;Жанр;Год;Страниц;Прочитано;Оценка");
                    foreach (var book in books)
                    {
                        writer.WriteLine($"{book.Id};{book.Title};{book.Author};{book.Genre};{book.Year};{book.Pages};{book.IsRead};{book.Rating}");
                    }
                }

                MessageBox.Show($"Экспорт завершён!\nФайл сохранён:\n{filePath}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}