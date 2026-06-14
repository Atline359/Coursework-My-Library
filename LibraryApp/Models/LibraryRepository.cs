using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace LibraryApp.Models
{
    // Хранилище книг (CRUD + JSON)
    public class LibraryRepository
    {
        private List<Book> _books;
        private readonly string _filePath;
        private int _nextId;

        public LibraryRepository()
        {
            _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.json");
            Load();
        }

        // Получить все книги
        public List<Book> GetAll() => _books.ToList();

        // Найти по ID
        public Book? GetById(int id) => _books.FirstOrDefault(b => b.Id == id);

        // Добавить
        public void Add(Book book)
        {
            book.Id = _nextId++;
            _books.Add(book);
            Save();
        }

        // Обновить
        public void Update(Book updatedBook)
        {
            var existing = GetById(updatedBook.Id);
            if (existing != null)
            {
                existing.Title = updatedBook.Title;
                existing.Author = updatedBook.Author;
                existing.Genre = updatedBook.Genre;
                existing.Year = updatedBook.Year;
                existing.Pages = updatedBook.Pages;
                existing.IsRead = updatedBook.IsRead;
                existing.Rating = updatedBook.Rating;
                Save();
            }
        }

        // Удалить
        public void Delete(int id)
        {
            var book = GetById(id);
            if (book != null)
            {
                _books.Remove(book);
                Save();
            }
        }

        // Сохранить в JSON
        private void Save()
        {
            var json = JsonConvert.SerializeObject(_books, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        // Загрузить из JSON
        private void Load()
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                _books = JsonConvert.DeserializeObject<List<Book>>(json) ?? new List<Book>();
                
                foreach (var book in _books)
                {
                    if (book.DateAdded == default)
                        book.DateAdded = DateTime.Now;
                }
            }
            else
            {
                _books = new List<Book>();
            }
            _nextId = _books.Any() ? _books.Max(b => b.Id) + 1 : 1;
        }
    }
}