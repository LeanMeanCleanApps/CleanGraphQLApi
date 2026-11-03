namespace CleanGraphQLApi.Presentation.GraphQL.Errors;

using Application.Common.Exceptions;

/// <summary>
/// Something the mutation needs doesn't exist, such as the review, its author or its movie.
/// </summary>
/// <param name="exception">The exception the Application layer raised.</param>
public sealed class NotFoundError(NotFoundException exception)
{
    /// <summary>
    /// What wasn't found.
    /// </summary>
    public string Message { get; } = exception.Message;
}
