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
        public ObservableCollection<string> Authors { get; set; }
        public ObservableCollection<string> Years { get; set; }

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
        
        private string _selectedAuthor = "Все";
        public string SelectedAuthor
        {
            get => _selectedAuthor;
            set
            {
                SetProperty(ref _selectedAuthor, value);
                ApplyFilters();
            }
        }

        private string _selectedYear = "Все";
        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                SetProperty(ref _selectedYear, value);
                ApplyFilters();
            }
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
            Authors = new ObservableCollection<string>();
            Years = new ObservableCollection<string>();
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
            CheckUnreadBooksReminder();
        }

        // Загрузить все книги
        private void LoadBooks()
        {
            var books = _repository.GetAll();
            Books.Clear();
            foreach (var book in books)
                Books.Add(book);
            ApplyFilters();
            UpdateFilters();
        }

        // Применить фильтры и сортировку
        private void ApplyFilters()
        {
            var filtered = _repository.GetAll().AsEnumerable();

            // Фильтр по жанру
            if (SelectedGenre != "Все")
                filtered = filtered.Where(b => b.Genre == SelectedGenre);

            // Фильтр по автору
            if (SelectedAuthor != "Все")
                filtered = filtered.Where(b => b.Author == SelectedAuthor);

            // Фильтр по году
            if (SelectedYear != "Все")
                filtered = filtered.Where(b => b.Year.ToString() == SelectedYear);

            // Фильтр "только непрочитанные"
            if (ShowOnlyUnread)
                filtered = filtered.Where(b => !b.IsRead);

            // Поиск по названию или автору
            if (!string.IsNullOrWhiteSpace(SearchText))
                filtered = filtered.Where(b => 
                    b.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    b.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            // Сортировка
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
        
        private void CheckUnreadBooksReminder()
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
    
            var oldUnreadBooks = _repository.GetAll()
                .Where(b => !b.IsRead && b.DateAdded <= thirtyDaysAgo)
                .ToList();
    
            if (oldUnreadBooks.Any())
            {
                var message = $"У вас есть {oldUnreadBooks.Count} книг(а), которые лежат непрочитанными больше 30 дней:\n\n";
                foreach (var book in oldUnreadBooks.Take(5))
                {
                    message += $"• {book.Title} - {book.Author} (добавлена {book.DateAdded:dd.MM.yyyy})\n";
                }
                if (oldUnreadBooks.Count > 5)
                    message += $"\n... и ещё {oldUnreadBooks.Count - 5} книг.";
        
                MessageBox.Show(message, "Напоминание", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Добавить книгу
        private void AddBook()
        {
            var newBook = new Book { Year = DateTime.Now.Year, DateAdded = DateTime.Now };
            var dialog = new BookDialogView(newBook);
            if (dialog.ShowDialog() == true)
            {
                _repository.Add(newBook);
                ApplyFilters(); 
            }
        }

        // Редактировать книгу
        private void EditBook()
        {
            if (SelectedBook == null) return;
    
            // Создаём копию книги для редактирования
            var bookToEdit = new Book
            {
                Id = SelectedBook.Id,
                Title = SelectedBook.Title,
                Author = SelectedBook.Author,
                Genre = SelectedBook.Genre,
                Year = SelectedBook.Year,
                Pages = SelectedBook.Pages,
                IsRead = SelectedBook.IsRead,
                Rating = SelectedBook.Rating,
                DateAdded = SelectedBook.DateAdded
            };
    
            var dialog = new BookDialogView(bookToEdit);
            if (dialog.ShowDialog() == true)
            {
                // Копируем изменённые данные обратно
                SelectedBook.Title = bookToEdit.Title;
                SelectedBook.Author = bookToEdit.Author;
                SelectedBook.Genre = bookToEdit.Genre;
                SelectedBook.Year = bookToEdit.Year;
                SelectedBook.Pages = bookToEdit.Pages;
                SelectedBook.IsRead = bookToEdit.IsRead;
                SelectedBook.Rating = bookToEdit.Rating;
        
                _repository.Update(SelectedBook);
                // Не вызываем LoadBooks(), а просто обновляем отображение
                ApplyFilters();
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
                ApplyFilters(); 
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
    
            ApplyFilters(); 
        }
        
        private void UpdateFilters()
        {
            var allBooks = _repository.GetAll();
    
            // Список авторов
            var authorsList = allBooks.Select(b => b.Author).Where(a => !string.IsNullOrWhiteSpace(a)).Distinct().OrderBy(a => a).ToList();
            Authors.Clear();
            Authors.Add("Все");
            foreach (var author in authorsList)
                Authors.Add(author);
    
            // Список годов
            var yearsList = allBooks.Select(b => b.Year).Distinct().OrderBy(y => y).ToList();
            Years.Clear();
            Years.Add("Все");
            foreach (var year in yearsList)
                Years.Add(year.ToString());
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