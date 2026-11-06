namespace CleanGraphQLApi.Presentation.Tests.Integration.GraphQL.Reviews;

using System;
using System.Net;
using System.Threading.Tasks;
using Common;
using Create;
using Delete;
using Shouldly;
using Update;
using Xunit;
using Review = Create.Review;
using Variables = Create.Variables;

public class ReviewTests : IDisposable
{
    private readonly GraphQLApplication application = new();

    #region Create

    private const string Errors = "errors{__typename,...on Error{message},...on ValidationError{fields{field,messages}}}";
    private const string CreateMutation = "mutation($review:CreateReviewInput!){createReview(input:$review){review{id,stars,author{id,firstName,lastName},movie{id,title}}," + Errors + "}}";
    private const string UpdateMutation = "mutation($review:UpdateReviewInput!){updateReview(input:$review){review{id,stars,author{id},movie{id}}," + Errors + "}}";
    private const string DeleteMutation = "mutation($review:DeleteReviewInput!){deleteReview(input:$review){reviewId," + Errors + "}}";

    [Fact]
    public async Task Reviews_ShouldFilter_ByTextAndStars()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={reviews(query:\"great\",minStars:4,first:50){nodes{id,stars}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        result.Data.Reviews.Nodes.ShouldNotBeEmpty();
        result.Data.Reviews.Nodes.ShouldAllBe(r => r.Stars >= 4);
    }

    [Fact]
    public async Task Create_ShouldCreate_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var authorResponse = await client.GetAsync("/graphql?query={authors(first:50){nodes{id,firstName,lastName}}}");
        var author = (await authorResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Authors.Nodes[0];
        using var movieResponse = await client.GetAsync("/graphql?query={movies(first:50){nodes{id,title}}}");
        var movie = (await movieResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Movies.Nodes[0];
        var content = CreateContent(author.Id, movie.Id, 5);

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.CreateReview.ShouldNotBeNull();
        payload.Errors.ShouldBeNull();

        var review = payload.Review.ShouldNotBeNull();
        review.Id.ShouldNotBe(Guid.Empty);
        review.Stars.ShouldBe(5);

        review.Author.Id.ShouldBe(author.Id);
        review.Author.FirstName.ShouldBe(author.FirstName);
        review.Author.LastName.ShouldBe(author.LastName);

        review.Movie.Id.ShouldBe(movie.Id);
        review.Movie.Title.ShouldBe(movie.Title);
    }

    [Fact]
    public async Task Create_ShouldReturn_ValidationError()
    {
        // Arrange
        using var client = this.application.CreateClient();
        var content = CreateContent(Guid.Empty, Guid.Empty, 5);

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.CreateReview.ShouldNotBeNull();
        payload.Review.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("ValidationError");
        error.Message.ShouldBe("One or more validation errors occurred.");
        error.Fields.ShouldNotBeNull().Select(f => f.Field).ShouldBe(["AuthorId", "MovieId"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_ShouldReturn_NotFoundError()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var movieResponse = await client.GetAsync("/graphql?query={movies(first:1){nodes{id}}}");
        var movie = (await movieResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Movies.Nodes[0];
        var content = CreateContent(Guid.NewGuid(), movie.Id, 5);

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.CreateReview.ShouldNotBeNull();
        payload.Review.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("NotFoundError");
        error.Message.ShouldBe("The Author with the supplied id was not found.");
    }

    [Fact]
    public async Task CreateNull_ShouldNotCreate_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        var content = new CreateReviewInputData
        {
            Query = CreateMutation,
        };

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _ = result.ShouldNotBeNull();

        result.Data.ShouldBeNull();

        result.Errors.ShouldNotBeEmpty();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Variable `review` is required.");
    }

    private static CreateReviewInputData CreateContent(Guid authorId, Guid movieId, int stars)
    {
        return new CreateReviewInputData
        {
            Query = CreateMutation,
            Variables = new Variables
            {
                Review = new Review
                {
                    AuthorId = authorId,
                    MovieId = movieId,
                    Stars = stars
                }
            }
        };
    }

    #endregion Create

    #region Delete

    [Fact]
    public async Task Delete_ShouldDelete_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var reviewResponse = await client.GetAsync("/graphql?query={reviews(first:50){nodes{id}}}");
        var reviewId = (await reviewResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Reviews.Nodes[0].Id;

        // Act
        using var response = await client.PostAsync($"/graphql", DeleteContent(reviewId).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.DeleteReview.ShouldNotBeNull();
        payload.Errors.ShouldBeNull();
        payload.ReviewId.ShouldBe(reviewId);

        using var deletedResponse = await client.GetAsync($"/graphql?query={{review(id:\"{reviewId}\"){{id}}}}");
        var deleted = (await deletedResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        deleted.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Extensions.Code.ShouldBe("NOT_FOUND");
    }

    [Fact]
    public async Task Delete_ShouldReturn_ValidationError()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.PostAsync($"/graphql", DeleteContent(Guid.Empty).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.DeleteReview.ShouldNotBeNull();
        payload.ReviewId.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("ValidationError");
        error.Fields.ShouldNotBeNull().ShouldHaveSingleItem().Field.ShouldBe("Id");
    }

    [Fact]
    public async Task Delete_ShouldReturn_NotFoundError()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.PostAsync($"/graphql", DeleteContent(Guid.NewGuid()).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.DeleteReview.ShouldNotBeNull();
        payload.ReviewId.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("NotFoundError");
        error.Message.ShouldBe("The Review with the supplied id was not found.");
    }

    [Fact]
    public async Task DeleteNull_ShouldNotDelete_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        var content = new DeleteReviewInputData
        {
            Query = DeleteMutation
        };

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _ = result.ShouldNotBeNull();

        result.Data.ShouldBeNull();

        result.Errors.ShouldNotBeEmpty();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Variable `review` is required.");
    }

    private static DeleteReviewInputData DeleteContent(Guid id)
    {
        return new DeleteReviewInputData
        {
            Query = DeleteMutation,
            Variables = new Delete.Variables
            {
                Review = new Delete.Review { Id = id }
            }
        };
    }

    #endregion Delete

    [Fact]
    public async Task Reviews_ShouldReturn_ReviewsList()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={reviews(first:50){nodes{id,stars,author{id,firstName,lastName},movie{id,title}}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        _ = result.Data.ShouldNotBeNull();

        result.Data.Reviews.Nodes.ShouldNotBeEmpty();
        result.Data.Reviews.Nodes.Length.ShouldBe(50);

        foreach (var review in result.Data.Reviews.Nodes)
        {
            _ = review.ShouldNotBeNull();
            _ = review.Id.ShouldBeOfType<Guid>();
            _ = review.Stars.ShouldBeOfType<int>();
            review.Stars.ShouldBeInRange(1, 5);

            _ = review.Author.ShouldNotBeNull();
            _ = review.Author.Id.ShouldBeOfType<Guid>();
            _ = review.Author.FirstName.ShouldBeOfType<string>();
            review.Author.FirstName.ShouldNotBeNullOrWhiteSpace();
            _ = review.Author.LastName.ShouldBeOfType<string>();
            review.Author.LastName.ShouldNotBeNullOrWhiteSpace();

            _ = review.Movie.ShouldNotBeNull();
            _ = review.Movie.Id.ShouldBeOfType<Guid>();
            _ = review.Movie.Title.ShouldBeOfType<string>();
            review.Movie.Title.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Review_ShouldReturn_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var setupResponse = await client.GetAsync("/graphql?query={reviews(first:50){nodes{id}}}");
        var setupResult = (await setupResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>();
        var reviewId = setupResult.Data.Reviews.Nodes[0].Id;

        // Act
        using var response = await client.GetAsync($"/graphql?query={{review(id:\"{reviewId}\"){{id,stars,author{{id,firstName,lastName}},movie{{id,title}}}}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        _ = result.Data.ShouldNotBeNull();

        _ = result.Data.Review.ShouldNotBeNull();
        _ = result.Data.Review.Id.ShouldBeOfType<Guid>();
        _ = result.Data.Review.Stars.ShouldBeOfType<int>();
        result.Data.Review.Stars.ShouldBeInRange(1, 5);

        _ = result.Data.Review.Author.ShouldNotBeNull();
        _ = result.Data.Review.Author.Id.ShouldBeOfType<Guid>();
        _ = result.Data.Review.Author.FirstName.ShouldBeOfType<string>();
        result.Data.Review.Author.FirstName.ShouldNotBeNullOrWhiteSpace();
        _ = result.Data.Review.Author.LastName.ShouldBeOfType<string>();
        result.Data.Review.Author.LastName.ShouldNotBeNullOrWhiteSpace();

        _ = result.Data.Review.Movie.ShouldNotBeNull();
        _ = result.Data.Review.Movie.Id.ShouldBeOfType<Guid>();
        _ = result.Data.Review.Movie.Title.ShouldBeOfType<string>();
    }

    #region Update

    [Fact]
    public async Task Update_ShouldUpdate_OnlyTheStars()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var reviewResponse = await client.GetAsync("/graphql?query={reviews(first:1){nodes{id,stars,author{id},movie{id}}}}");
        var review = (await reviewResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Reviews.Nodes[0];
        var stars = review.Stars == 5 ? 1 : review.Stars + 1;

        // Act
        using var response = await client.PostAsync($"/graphql", UpdateContent(review.Id, stars).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.UpdateReview.ShouldNotBeNull();
        payload.Errors.ShouldBeNull();

        var updated = payload.Review.ShouldNotBeNull();
        updated.Id.ShouldBe(review.Id);
        updated.Stars.ShouldBe(stars);
        updated.Author.Id.ShouldBe(review.Author.Id);
        updated.Movie.Id.ShouldBe(review.Movie.Id);

        using var savedResponse = await client.GetAsync($"/graphql?query={{review(id:\"{review.Id}\"){{stars}}}}");
        var saved = (await savedResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Review;

        saved.Stars.ShouldBe(stars);
    }

    [Fact]
    public async Task Update_ShouldReturn_NotFoundError()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.PostAsync($"/graphql", UpdateContent(Guid.NewGuid(), 5).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.UpdateReview.ShouldNotBeNull();
        payload.Review.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("NotFoundError");
        error.Message.ShouldBe("The Review with the supplied id was not found.");
    }

    [Fact]
    public async Task Update_ShouldReturn_ValidationError()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.PostAsync($"/graphql", UpdateContent(Guid.Empty, 6).ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Errors.ShouldBeNull();

        var payload = result.Data.UpdateReview.ShouldNotBeNull();
        payload.Review.ShouldBeNull();

        var error = payload.Errors.ShouldNotBeNull().ShouldHaveSingleItem();
        error.TypeName.ShouldBe("ValidationError");
        error.Message.ShouldBe("One or more validation errors occurred.");
        error.Fields.ShouldNotBeNull().Select(f => f.Field).ShouldBe(["Id", "Stars"], ignoreOrder: true);
    }

    [Fact]
    public async Task UpdateNull_ShouldNotUpdate_Review()
    {
        // Arrange
        using var client = this.application.CreateClient();
        var content = new UpdateReviewInputData()
        {
            Query = UpdateMutation
        };

        // Act
        using var response = await client.PostAsync($"/graphql", content.ToStringContent());
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        _ = result.ShouldNotBeNull();

        result.Data.ShouldBeNull();

        result.Errors.ShouldNotBeEmpty();
        result.Errors.Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("Variable `review` is required.");
    }

    private static UpdateReviewInputData UpdateContent(Guid id, int stars)
    {
        return new UpdateReviewInputData()
        {
            Query = UpdateMutation,
            Variables = new Update.Variables
            {
                Review = new Update.Review
                {
                    Id = id,
                    Stars = stars
                }
            }
        };
    }

    #endregion Update

    #region Paging

    [Fact]
    public async Task Reviews_ShouldFilter_ByMovie()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var setupResponse = await client.GetAsync("/graphql?query={reviews(first:50){nodes{movie{id}}}}");
        var movieId = (await setupResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Reviews.Nodes[0].Movie.Id;

        // Act
        using var response = await client.GetAsync($"/graphql?query={{reviews(movieId:\"{movieId}\",first:50){{totalCount,nodes{{id,stars,movie{{id}}}}}}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        result.Errors.ShouldBeNull();

        var page = result.Data.Reviews;
        page.TotalCount.ShouldBeGreaterThan(0);
        page.Nodes.ShouldAllBe(r => r.Movie.Id == movieId);
    }

    [Fact]
    public async Task Reviews_ShouldFilter_ByAuthor()
    {
        // Arrange
        using var client = this.application.CreateClient();
        using var setupResponse = await client.GetAsync("/graphql?query={reviews(first:50){nodes{author{id}}}}");
        var authorId = (await setupResponse.Content.ReadAsStringAsync()).Deserialize<GraphData>().Data.Reviews.Nodes[0].Author.Id;

        // Act
        using var response = await client.GetAsync($"/graphql?query={{reviews(authorId:\"{authorId}\",first:50){{totalCount,nodes{{id,author{{id}}}}}}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var page = result.Data.Reviews;
        page.TotalCount.ShouldBeGreaterThan(0);
        page.Nodes.ShouldAllBe(r => r.Author.Id == authorId);
    }

    [Fact]
    public async Task Reviews_ShouldReturn_ValidationError_ForInvalidCursor()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={reviews(after:\"not-a-cursor\"){totalCount}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        result.Data.Reviews.ShouldBeNull();

        result.Errors.ShouldNotBeNull().Count.ShouldBe(1);
        result.Errors[0].Message.ShouldBe("One or more validation errors occurred.");
        result.Errors[0].Extensions.Code.ShouldBe("VALIDATION");
        result.Errors[0].Extensions.Errors["After"].ShouldBe(["The cursor is not valid. Use the endCursor from a previous page, or leave it empty for the first page."]);
    }

    [Fact]
    public async Task Reviews_ShouldReturn_Error_WhenPageIsTooLarge()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={reviews(first:51){totalCount}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        result.Data.Reviews.ShouldBeNull();

        result.Errors.ShouldNotBeNull().Count.ShouldBe(1);
        result.Errors[0].Extensions.Code.ShouldBe("HC0051");
    }

    #endregion Paging

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this.application.Dispose();
        }
    }
}
