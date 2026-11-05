namespace CleanGraphQLApi.Presentation.GraphQL.Mutations;

using Application.Reviews.Entities;
using Errors;
#if includeAuth
using HotChocolate.Authorization;
#endif
using HotChocolate.Types;
using Mediator;
using Commands = Application.Reviews.Commands;

/// <summary>
/// The review mutations. Each one follows Hot Chocolate's mutation conventions: it takes one input, named
/// <c>&lt;Mutation&gt;Input</c>, and returns a payload, named <c>&lt;Mutation&gt;Payload</c>, holding either the result or
/// the typed errors the caller can expect.
/// </summary>
[MutationType]
public static partial class ReviewMutations
{
    /// <summary>
    /// Creates a review of a movie and returns it.
    /// </summary>
    /// <param name="authorId">The unique identifier of the author writing the review.</param>
    /// <param name="movieId">The unique identifier of the movie being reviewed.</param>
    /// <param name="stars">The star rating, from 1 to 5.</param>
#if includeAuth
    [Authorize]
#endif
    [Error<ValidationError>]
    [Error<NotFoundError>]
    public static async Task<Review> CreateReview(
        Guid authorId,
        Guid movieId,
        int stars,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new Commands.CreateReview.CreateReviewCommand
        {
            AuthorId = authorId,
            MovieId = movieId,
            Stars = stars
        }, cancellationToken);
    }

    /// <summary>
    /// Changes the star rating of a review and returns the updated review. The author and the movie of a review can't change.
    /// </summary>
    /// <param name="id">The unique identifier of the review.</param>
    /// <param name="stars">The new star rating, from 1 to 5.</param>
#if includeAuth
    [Authorize]
#endif
    [Error<ValidationError>]
    [Error<NotFoundError>]
    public static async Task<Review> UpdateReview(
        Guid id,
        int stars,
        ISender sender,
        CancellationToken cancellationToken)
    {
        return await sender.Send(new Commands.UpdateReview.UpdateReviewCommand
        {
            Id = id,
            Stars = stars
        }, cancellationToken);
    }

    /// <summary>
    /// Deletes a review and returns the id it had.
    /// </summary>
    /// <param name="id">The unique identifier of the review.</param>
#if includeAuth
    [Authorize]
#endif
    [Error<ValidationError>]
    [Error<NotFoundError>]
    [UseMutationConvention(PayloadFieldName = "reviewId")]
    public static async Task<Guid> DeleteReview(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        _ = await sender.Send(new Commands.DeleteReview.DeleteReviewCommand { Id = id }, cancellationToken);

        return id;
    }
}
