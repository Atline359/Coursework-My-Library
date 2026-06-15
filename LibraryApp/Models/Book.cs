namespace LibraryApp.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Pages { get; set; }
        public bool IsRead { get; set; }
        public int Rating { get; set; }
        public DateTime DateAdded { get; set; }
    }
}