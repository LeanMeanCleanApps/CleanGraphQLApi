namespace CleanGraphQLApi.Presentation.Tests.Unit.GraphQL.Errors;

using CleanGraphQLApi.Application.Common.Exceptions;
using CleanGraphQLApi.Presentation.GraphQL.Errors;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate;
using Shouldly;
using Xunit;

public class ApplicationErrorFilterTests
{
    private readonly ApplicationErrorFilter filter = new();

    [Fact]
    public void OnError_ShouldReturn_ValidationError()
    {
        // Arrange
        var exception = new ValidationException(
        [
            new ValidationFailure("Id", "A Review Id was not supplied."),
            new ValidationFailure("Stars", "A star rating must be between 1 and 5."),
            new ValidationFailure("Stars", "A star rating must be between 1 and 5.")
        ]);

        // Act
        var result = this.filter.OnError(Error(exception));

        // Assert
        result.Code.ShouldBe("VALIDATION");
        result.Message.ShouldBe("One or more validation errors occurred.");

        var errors = result.Extensions.ShouldNotBeNull()["errors"].ShouldBeOfType<Dictionary<string, object?>>();

        errors.Count.ShouldBe(2);
        errors["Id"].ShouldBeOfType<string[]>().ShouldBe(["A Review Id was not supplied."]);
        errors["Stars"].ShouldBeOfType<string[]>().ShouldBe(["A star rating must be between 1 and 5."]);
    }

    [Fact]
    public void OnError_ShouldReturn_NotFoundError()
    {
        // Arrange
        var exception = new NotFoundException("Review not found");

        // Act
        var result = this.filter.OnError(Error(exception));

        // Assert
        result.Code.ShouldBe("NOT_FOUND");
        result.Message.ShouldBe("Review not found");
    }

    [Fact]
    public void OnError_ShouldReturn_SameError_WhenExceptionIsUnexpected()
    {
        // Arrange
        var error = Error(new ArgumentException("Expected Exception"));

        // Act
        var result = this.filter.OnError(error);

        // Assert
        result.ShouldBeSameAs(error);
    }

    [Fact]
    public void OnError_ShouldReturn_SameError_WhenThereIsNoException()
    {
        // Arrange
        var error = ErrorBuilder.New().SetMessage("Unexpected Execution Error").Build();

        // Act
        var result = this.filter.OnError(error);

        // Assert
        result.ShouldBeSameAs(error);
    }

    private static IError Error(Exception exception)
    {
        return ErrorBuilder.New()
            .SetMessage("Unexpected Execution Error")
            .SetException(exception)
            .Build();
    }
}
