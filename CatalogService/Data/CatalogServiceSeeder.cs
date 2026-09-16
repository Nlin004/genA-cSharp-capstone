using CatalogService.Models;

namespace CatalogService.Data;

public static class CatalogServiceSeeder
{
    public static void Seed(CatalogServiceContext db)
    {
        if (db.Books.Any())
            return;

        var books = new List<Book>
        {
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-06-112008-4",
                Title = "To Kill a Mockingbird",
                Author = "Harper Lee",
                Genre = "Fiction",
                PublicationYear = 1960,
                Description = "A story of racial injustice and childhood innocence in the American South.",
                Publisher = "J. B. Lippincott & Co.",
                PageCount = 281,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 3
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-7432-7356-5",
                Title = "1984",
                Author = "George Orwell",
                Genre = "Dystopian Fiction",
                PublicationYear = 1949,
                Description = "A chilling portrait of a totalitarian society under constant surveillance.",
                Publisher = "Secker & Warburg",
                PageCount = 328,
                Language = "English",
                TotalCopies = 4,
                AvailableCopies = 4
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-7432-7357-2",
                Title = "The Great Gatsby",
                Author = "F. Scott Fitzgerald",
                Genre = "Fiction",
                PublicationYear = 1925,
                Description = "The story of the mysteriously wealthy Jay Gatsby and his love for Daisy Buchanan.",
                Publisher = "Charles Scribner's Sons",
                PageCount = 180,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 2
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-316-76948-0",
                Title = "The Catcher in the Rye",
                Author = "J.D. Salinger",
                Genre = "Fiction",
                PublicationYear = 1951,
                Description = "A teenage boy's journey through New York City after being expelled from prep school.",
                Publisher = "Little, Brown and Company",
                PageCount = 277,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 2
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-14-028329-7",
                Title = "Of Mice and Men",
                Author = "John Steinbeck",
                Genre = "Fiction",
                PublicationYear = 1937,
                Description = "Two displaced ranch workers seek their dream of owning land during the Great Depression.",
                Publisher = "Covici Friede",
                PageCount = 112,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 3
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-06-093546-9",
                Title = "To the Lighthouse",
                Author = "Virginia Woolf",
                Genre = "Modernist Fiction",
                PublicationYear = 1927,
                Description = "A landmark modernist novel depicting the Ramsay family and their visits to the Isle of Skye.",
                Publisher = "Hogarth Press",
                PageCount = 209,
                Language = "English",
                TotalCopies = 1,
                AvailableCopies = 0
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-385-49081-5",
                Title = "The Da Vinci Code",
                Author = "Dan Brown",
                Genre = "Mystery",
                PublicationYear = 2003,
                Description = "A symbologist uncovers a conspiracy involving the Holy Grail and a secret society.",
                Publisher = "Doubleday",
                PageCount = 454,
                Language = "English",
                TotalCopies = 5,
                AvailableCopies = 5
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-439-02348-1",
                Title = "Harry Potter and the Sorcerer's Stone",
                Author = "J.K. Rowling",
                Genre = "Fantasy",
                PublicationYear = 1997,
                Description = "A young boy discovers he is a wizard and begins his education at Hogwarts School.",
                Publisher = "Scholastic",
                PageCount = 309,
                Language = "English",
                TotalCopies = 6,
                AvailableCopies = 6
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-7432-7356-9",
                Title = "Dune",
                Author = "Frank Herbert",
                Genre = "Science Fiction",
                PublicationYear = 1965,
                Description = "An epic science fiction tale of politics, religion, and ecology on a desert planet.",
                Publisher = "Chilton Books",
                PageCount = 688,
                Language = "English",
                TotalCopies = 3,
                AvailableCopies = 3
            },
            new()
            {
                BookId = Guid.NewGuid(),
                Isbn = "978-0-452-28423-4",
                Title = "Brave New World",
                Author = "Aldous Huxley",
                Genre = "Dystopian Fiction",
                PublicationYear = 1932,
                Description = "A vision of a future society where citizens are engineered and conditioned for happiness.",
                Publisher = "Chatto & Windus",
                PageCount = 311,
                Language = "English",
                TotalCopies = 2,
                AvailableCopies = 2
            }
        };

        db.Books.AddRange(books);
        db.SaveChanges();
    }
}