using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LibraryApp;

public abstract class User
{
    public string Name { get; protected set; }
    public abstract void ShowMenu(Library library);
}

public class Librarian : User
{
    private readonly string password;
    public Librarian(string n, string p) { Name = n; this.password = p; }
    public bool VerifyPassword(string p) => this.password == p;
    public override void ShowMenu(Library lib)
    {
        while (true)
        {
            Console.WriteLine("\nМеню библиотекаря:\n1. Добавить книгу\n2. Удалить книгу\n3. Добавить пользователя\n4. Просмотреть всех пользователей\n5. Просмотреть все книги\n6. Выход");
            switch (Console.ReadLine())
            {
                case "1": lib.AddBook(); break;
                case "2": lib.RemoveBook(); break;
                case "3": lib.AddUser(); break;
                case "4": lib.ViewAllUsers(); break;
                case "5": lib.ViewAllBooks(); break;
                case "6": return;
                default: Console.WriteLine("Неверный выбор."); break;
            }
        }
    }
}

public class LibraryUser : User
{
    public List<string> BorrowedBooks { get; set; } = new();
    public LibraryUser(string name) => Name = name;
    public override void ShowMenu(Library lib)
    {
        while (true)
        {
            Console.WriteLine("\nМеню пользователя:\n1. Просмотреть доступные\n2. Взять книгу\n3. Вернуть книгу\n4. Просмотреть взятые\n5. Выход");
            switch (Console.ReadLine())
            {
                case "1": lib.ViewAvailableBooks(); break;
                case "2": lib.BorrowBook(this); break;
                case "3": lib.ReturnBook(this); break;
                case "4": ViewBorrowedBooks(); break;
                case "5": return;
                default: Console.WriteLine("Неверный выбор."); break;
            }
        }
    }
    void ViewBorrowedBooks() => Console.WriteLine(BorrowedBooks.Count == 0 ? "\nНет взятых книг." : $"\nВзятые книги:\n{string.Join("\n", BorrowedBooks.Select(b => $"- {b}"))}");
}

public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public bool IsAvailable { get; set; } = true;
    public override string ToString() => $"{Title} от {Author} ({(IsAvailable ? "Доступна" : "Взята")})";
}

public class Library
{
    private readonly List<Book> books = new();
    private readonly List<LibraryUser> users = new();
    private readonly List<Librarian> librarians = new();
    private const string BooksFile = "books.txt";
    private const string UsersFile = "users.txt";
    private const string LibrariansFile = "librarians.txt";

    public Library()
    {
        AddLibrarian("admin", "123123");
        LoadData();
    }

    void LoadData() { LoadBooks(); LoadUsers(); LoadLibrarians(); }

    void LoadLibrarians()
    {
        if (!File.Exists(LibrariansFile)) return;
        var lines = File.ReadAllLines(LibrariansFile);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length == 2 && !(parts[0] == "admin" && parts[1] == "123123"))
            {
                AddLibrarian(parts[0], parts[1]);
            }
        }
    }

    void LoadBooks()
    {
        if (!File.Exists(BooksFile)) return;
        var lines = File.ReadAllLines(BooksFile);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length == 3)
            {
                books.Add(new() { Title = parts[0], Author = parts[1], IsAvailable = bool.TryParse(parts[2], out var isAvail) ? isAvail : true });
            }
        }
    }

    void LoadUsers()
    {
        if (!File.Exists(UsersFile)) return;
        var lines = File.ReadAllLines(UsersFile);
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
            {
                var user = new LibraryUser(parts[0]);
                users.Add(user);
                if (parts.Length > 1)
                {
                    user.BorrowedBooks.AddRange(parts[1].Split(','));
                }
            }
        }
    }

    public void SaveData()
    {
        File.WriteAllLines(BooksFile, books.Select(b => $"{b.Title}|{b.Author}|{b.IsAvailable}"));
        File.WriteAllLines(UsersFile, users.Select(u => $"{u.Name}|{string.Join(",", u.BorrowedBooks)}"));
        File.WriteAllLines(LibrariansFile, librarians.Select(l => $"{l.Name}|{l.VerifyPassword}"));
    }

    public void AddBook()
    {
        Console.Write("Введите название: ");
        string title = Console.ReadLine();
        Console.Write("Введите автора: ");
        string author = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author)) { Console.WriteLine("Название/Автор не может быть пустым."); return; }
        books.Add(new Book { Title = title, Author = author });
        Console.WriteLine("Книга добавлена.");
    }

    public void RemoveBook()
    {
        Console.Write("Введите название книги для удаления: ");
        string title = Console.ReadLine();
        var bookToRemove = books.FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        if (bookToRemove != null) { books.Remove(bookToRemove); Console.WriteLine("Книга удалена."); }
        else Console.WriteLine("Книга не найдена.");
    }

    public void AddUser()
    {
        Console.Write("Введите имя пользователя: ");
        string name = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(name)) { Console.WriteLine("Имя пользователя не может быть пустым."); return; }
        if (GetUserByName(name) != null) { Console.WriteLine("Пользователь с таким именем уже существует."); return; }
        users.Add(new LibraryUser(name));
        Console.WriteLine("Пользователь добавлен.");
    }

    public void ViewAllUsers() => users.ForEach(u => Console.WriteLine($"- {u.Name}"));
    public void ViewAllBooks() => books.ForEach(book => Console.WriteLine($"- {book}"));
    public void ViewAvailableBooks() => books.ForEach(b => Console.WriteLine($"- {b.Title} от {b.Author}"));

    public void BorrowBook(LibraryUser user)
    {
        Console.Write("Введите название книги для взятия: ");
        string title = Console.ReadLine();
        var bookToBorrow = books.FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && b.IsAvailable);

        if (bookToBorrow != null)
        {
            bookToBorrow.IsAvailable = false;
            user.BorrowedBooks.Add(bookToBorrow.Title);
            Console.WriteLine("Книга взята.");
        }
        else Console.WriteLine("Книга недоступна/не найдена.");
    }

    public void ReturnBook(LibraryUser user)
    {
        Console.Write("Введите название книги для возврата: ");
        string title = Console.ReadLine();
        var bookToReturn = books.FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase) && !b.IsAvailable);

        if (bookToReturn != null && user.BorrowedBooks.Contains(bookToReturn.Title))
        {
            bookToReturn.IsAvailable = true;
            user.BorrowedBooks.Remove(bookToReturn.Title);
            Console.WriteLine("Книга возвращена.");
        }
        else Console.WriteLine("Книга не была взята или не найдена.");
    }

    public List<LibraryUser> GetUsers() => users;
    public List<Librarian> GetLibrarians() => librarians;
    public LibraryUser GetUserByName(string name) => users.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public Librarian GetLibrarianByName(string name) => librarians.FirstOrDefault(l => l.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    public void AddLibrarian(string n, string p) { if (librarians.All(l => l.Name != n)) librarians.Add(new Librarian(n, p)); }
    public void RegisterLibrarian(string n, string p) => AddLibrarian(n, p);
}

class Program
{
    static User Authenticate(Library lib)
    {
        while (true)
        {
            Console.WriteLine("\nВыберите вход:\n1. Библиотекарь\n2. Пользователь\nВведите: ");
            switch (Console.ReadLine())
            {
                case "1":
                    Console.Write("Имя библиотекаря: ");
                    string libName = Console.ReadLine();
                    Console.Write("Пароль: ");
                    string libPass = Console.ReadLine();
                    var librarian = lib.GetLibrarianByName(libName);
                    if (librarian != null && librarian.VerifyPassword(libPass)) return librarian;
                    Console.WriteLine("Неверные учетные данные.");
                    break;
                case "2":
                    Console.Write("Имя пользователя: ");
                    string userName = Console.ReadLine();
                    var user = lib.GetUserByName(userName);
                    if (user is null)
                    {
                        Console.WriteLine("Пользователь не найден. Вы должны быть добавлены библиотекарем.");
                        return null;
                    }
                    return user;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }
    }

    static void Main(string[] args)
    {
        var lib = new Library();
        while (true)
        {
            Console.WriteLine("\nДобро пожаловать в библиотеку!");
            var currentUser = Authenticate(lib);
            if (currentUser != null) currentUser.ShowMenu(lib);
            else Console.WriteLine("Аутентификация не удалась.");
            lib.SaveData();
        }
    }
}