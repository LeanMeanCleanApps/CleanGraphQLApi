namespace CleanGraphQLApi.Presentation.GraphQL.Errors;

using Application.Common.Exceptions;
using FluentValidation;
using HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maps exceptions raised by the Application layer to GraphQL errors. Any other exception is logged here as an
/// error, because Hot Chocolate only returns it to the client.
/// </summary>
/// <param name="logger">Where unexpected exceptions are logged.</param>
public partial class ApplicationErrorFilter(ILogger<ApplicationErrorFilter> logger) : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            ValidationException validationException => ErrorBuilder.FromError(error)
                .SetMessage("One or more validation errors occurred.")
                .SetCode("VALIDATION")
                .SetExtension("errors", validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, object? (g) => g.Select(e => e.ErrorMessage).Distinct().ToArray()))
                .Build(),
            NotFoundException notFoundException => ErrorBuilder.FromError(error)
                .SetMessage(notFoundException.Message)
                .SetCode("NOT_FOUND")
                .Build(),
            Exception exception => this.Unexpected(error, exception),
            _ => error
        };
    }

    private IError Unexpected(IError error, Exception exception)
    {
        LogUnexpected(logger, exception, error.Path?.ToString());

        return error;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "An unexpected exception stopped {FieldPath}")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, string? fieldPath);
}
