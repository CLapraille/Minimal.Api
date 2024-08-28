using FluentValidation;
using Library.Api.Models;

namespace Library.Api.Validators
{
    public class BookValidator : AbstractValidator<Book>
    {
        public BookValidator()
        {
            RuleFor(book => book.Isbn)
                .Matches(@"(?=(?:\D*\d){10}(?:(?:\D*\d){3})?$)")
                .WithMessage("Value was not a valide ISBN 13");

            RuleFor(book => book.Title).NotEmpty();
            RuleFor(book => book.ShortDescription).NotEmpty();
            RuleFor(book => book.PageCount).GreaterThan(0);
            RuleFor(book => book.Author).NotEmpty();
        }
    }
}
