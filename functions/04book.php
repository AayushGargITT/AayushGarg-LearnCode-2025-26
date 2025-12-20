<?php
class Book {
    public function getTitle(): string {
        return "A Great Book";
    }

    public function getAuthor(): string {
        return "John Doe";
    }

    public function turnPage(): void {
        // pointer to next page
    }

    public function getCurrentPage(): string {
        return "current page content";
    }

    public function getLocation(): string {
        // returns the position in the library ie. shelf number & room number
        return "Shelf 1, Room 100";
    }
}

interface IBookRepository
{
    public function save(Book $book):void;
}


class BookRepository implements IBookREpository{
    public function save(Book $book): void {
        $filename = '/documents/' . $book->getTitle() . ' - ' . $book->getAuthor();
        file_put_contents($filename, serialize($book));
    }
}

interface Printer {
    public function printPage(string $page): void;
}

class PlainTextPrinter implements Printer {
    public function printPage(string $page): void {
        echo $page;
    }
}

class HtmlPrinter implements Printer {
    public function printPage(string $page): void {
        echo '<div style="single-page">' . $page . '</div>';
    }
}
