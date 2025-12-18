public class Book {
 
    public string getTitle() {
        return "A Great Book";
    }
 
    public string getAuthor() {
        return "John Doe";
    }
 
    public void turnPage() {
        // pointer to next page
    }
 
    public string getCurrentPage() {
        return "current page content";
    }
 
    public string getLocation() {
        // returns the position in the library
        // ie. shelf number & room number
    }
}
public class BookRepository{
    public void save(Book book) {
        filename =  $"documents/{book.GetTitle()} - {book.GetAuthor()}";
        file_put_contents(filename, JsonSerializer.Serialize(book));
    }
}

interface Printer {
 
    function printPage(int page);
}
 
public class PlainTextPrinter : Printer {
 
    public void printPage(int page) {
        Console.WriteLine(page);
    }
 
}
 
public class HtmlPrinter :Printer {
 
    public void printPage(int page) {
        Console.WriteLine($"<div style=\"single-page\">{page}</div>");
    }
}