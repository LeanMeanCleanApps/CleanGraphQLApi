namespace CleanGraphQLApi.Presentation.GraphQL.Queries;

using Application.Common.Paging;
using Application.Reviews.Entities;
using Extensions;
#if includeAuth
using HotChocolate.Authorization;
#endif
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Mediator;
using Reviews = Application.Reviews.Queries;

[QueryType]
public static partial class ReviewQueries
{
    /// <summary>
    /// Gets a review by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the review.</param>
#if includeAuth
    [Authorize]
#endif
    public static async Task<Review?> GetReview(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        return await sender.Send(new Reviews.GetReviewById.GetReviewByIdQuery { Id = id }, cancellationToken);
    }

    /// <summary>
    /// Lists reviews ordered by movie then author, one page at a time, optionally only those of one movie, by one author, or both.
    /// </summary>
    /// <param name="movieId">Only return reviews of this movie. Leave empty for every movie.</param>
    /// <param name="authorId">Only return reviews by this author. Leave empty for every author.</param>
    /// <param name="query">Only return reviews whose text contains this, ignoring case. Leave empty for every review.</param>
    /// <param name="minStars">Only return reviews with at least this many stars, from 1 to 5. Leave empty for every rating.</param>
    [UsePaging(ConnectionName = "Reviews")]
#if includeAuth
    [Authorize]
#endif
    public static async Task<Connection<Review>> GetReviews(
        Guid? movieId,
        Guid? authorId,
        string? query,
        int? minStars,
        int? first,
        string? after,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var page = await sender.Send(new Reviews.GetReviews.GetReviewsQuery
        {
            MovieId = movieId,
            AuthorId = authorId,
            Query = query,
            MinStars = minStars,
            Offset = after.ToOffset(),
            Limit = first ?? Paging.DefaultLimit
        }, cancellationToken);

        return page.ToConnection();
    }
}
