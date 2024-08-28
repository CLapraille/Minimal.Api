using FluentAssertions;
using Library.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Library.Api.Test.Integration
{
    public class LibraryEndpointsTests 
        : IClassFixture<LibraryApiFactory>, IAsyncLifetime
    {
        private readonly LibraryApiFactory _factory;
        private readonly List<string> _createdIsbns = new();
        public LibraryEndpointsTests(LibraryApiFactory factory)
        {
            _factory = factory;
        }


        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            var httpClient = _factory.CreateClient();

            foreach (string isbn in _createdIsbns)
                await httpClient.DeleteAsync($"/books/{isbn}");
        }


        [Fact]
        public async Task CreateBook_CreatesBook_WhenDataIsCorrect()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();


            // Act
            var result = await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var createdBook = await result.Content.ReadFromJsonAsync<Book>();
            

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.Created);
            createdBook.Should().BeEquivalentTo(book);
            result.Headers.Location.Should().Be($"{httpClient.BaseAddress!.OriginalString}/books/{book.Isbn}");
        }

        [Fact]
        public async Task CreateBook_Fails_WhenIsbnIsInvalid()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            book.Isbn = "INVALID";

            // Act
            var result = await httpClient.PostAsJsonAsync("/books", book);
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Isbn");
            error.ErrorMessage.Should().Be("Value was not a valide ISBN 13");
        }

        [Fact]
        public async Task CreateBook_Fails_WhenBookExists()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            // Act
            await httpClient.PostAsJsonAsync("/books", book);
            var result = await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Isbn");
            error.ErrorMessage.Should().Be("A book with this ISBN-13 already exists");
        }


        [Fact]
        public async Task GetBook_ReturnsBook_WhenBookExists()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            // Act
            var result = await httpClient.GetAsync($"/books/{book.Isbn}");
            var existingBook = await result.Content.ReadFromJsonAsync<Book>();

            // Assert
            existingBook.Should().BeEquivalentTo(book);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GetBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            string isbn = GenerateIsbn();
            
            // Act
            var result = await httpClient.GetAsync($"/books/{isbn}");            

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAllBooks_ReturnsAllBooks_WhenBooksExist()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var books = new List<Book>() { book };

            // Act
            var result = await httpClient.GetAsync($"/books");
            var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();
            
            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            returnedBooks.Should().BeEquivalentTo(books);
        }

        [Fact]
        public async Task GetAllBooks_ReturnsNoBooks_WhenNoBooksExist()
        {
            // Arrange
            var httpClient = _factory.CreateClient();     

            // Act
            var result = await httpClient.GetAsync($"/books");
            var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            returnedBooks.Should().BeEmpty();
        }


        [Fact]
        public async Task SearchBooks_ReturnsBooks_WhenTitleMatches()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook("Integration Testing for Search Endpoint");
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);
            var books = new List<Book>() { book };

            // Act
            var result = await httpClient.GetAsync($"/books?searchTerm=arch");
            var returnedBooks = await result.Content.ReadFromJsonAsync<List<Book>>();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            returnedBooks.Should().BeEquivalentTo(books);
        }

        [Fact]
        public async Task UpdateBook_UpdatesBook_WhenDataIsCorrect()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            // Act
            book.PageCount = 1000;
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
            var updatedBook = await result.Content.ReadFromJsonAsync<Book>();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            updatedBook.Should().BeEquivalentTo(book);
        }

        [Fact]
        public async Task UpdateBook_DoesNotUpdateBook_WhenDataIsIncorrect()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            // Act
            book.Title = string.Empty;
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);
            var errors = await result.Content.ReadFromJsonAsync<IEnumerable<ValidationError>>();
            var error = errors!.Single();

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            error.PropertyName.Should().Be("Title");
            error.ErrorMessage.Should().Be("'Title' must not be empty.");
        }

        [Fact]
        public async Task UpdateBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();

            // Act
            var result = await httpClient.PutAsJsonAsync($"/books/{book.Isbn}", book);

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact]
        public async Task DeleteBook_ReturnsNotFound_WhenBookDoesNotExist()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            string isbn = GenerateIsbn();

            // Act
            var result = await httpClient.DeleteAsync($"/books/{isbn}");

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteBook_ReturnsNoContent_WhenBookExists()
        {
            // Arrange
            var httpClient = _factory.CreateClient();
            var book = GenerateBook();
            await httpClient.PostAsJsonAsync("/books", book);
            _createdIsbns.Add(book.Isbn);

            // Act
            var result = await httpClient.DeleteAsync($"/books/{book.Isbn}");

            // Assert
            result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }


        private Book GenerateBook(string title = "Integration Testing")
        {
            return new Book
            {
                Isbn = GenerateIsbn(),
                Title = title,
                Author = "Chris Lapraille",
                PageCount = 450,
                ShortDescription = "My Best Book",
                ReleaseDate = new DateTime(2024, 8, 22)
            };
        }

        private string GenerateIsbn()
        {
            return $"{Random.Shared.Next(100, 999)}-" +
                   $"{Random.Shared.Next(1000000000, 2100999999)}";
        }

        
    }
}
