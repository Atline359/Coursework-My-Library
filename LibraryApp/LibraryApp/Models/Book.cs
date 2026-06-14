namespace LibraryApp.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string Genre { get; set; } = "";
        public int Year { get; set; }
        public int Pages { get; set; }
        public bool IsRead { get; set; }
        public int Rating { get; set; }
    }
}