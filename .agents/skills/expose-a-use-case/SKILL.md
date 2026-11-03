---
name: expose-a-use-case
description: How to put an Application use case on the Hot Chocolate GraphQL schema - static partial query and mutation classes, source-generated types, mutation conventions with typed errors, Relay connections, authorization, XML comments as schema descriptions, and tests. Use when adding or changing a query, mutation, field or type in this GraphQL API.
metadata:
  scope: presentation
---

# Expose a use case (GraphQL)

## The rule

The schema is built from code by Hot Chocolate's source generator (`HotChocolate.Types.Analyzers`), registered in one call: `AddPresentationTypes()` in `src/Presentation/Extensions/ProgramExtensions.cs`. Everything lives in `src/Presentation/GraphQL/`:

| Folder | Holds | Shape |
| --- | --- | --- |
| `Queries/` | Root query fields, one class per feature | `[QueryType] public static partial class MovieQueries` |
| `Mutations/` | Root mutation fields | `[MutationType] public static partial class ReviewMutations` |
| `Types/` | How an Application entity appears in the schema | `[ObjectType<Movie>] public static partial class MovieType` with `static partial void Configure(...)` |
| `Errors/` | Typed mutation errors and the error filter | `api-conventions` |
| `DataLoaders/` | Batched lookups for fields on list items | `dataloaders` |

Each field method is `public static async`, takes its arguments, then `ISender sender` and `CancellationToken cancellationToken`, builds the Application request, sends it and returns the result. Fields hold no rules: validation and not-found come from Application. A mutation's `[Error<T>]` attributes turn them into typed errors in its payload, and `ApplicationErrorFilter` turns them into GraphQL errors everywhere else (`api-conventions`).

Hot Chocolate reads XML comments, so a field's `<summary>` and `<param>` tags and an entity record's `<param>` tags become the schema descriptions.

## Steps

1. Add the use case first (`add-a-use-case`).
2. Add the field to the feature's class in `Queries/` or `Mutations/`. The method name minus `Get` and `Async` becomes the field name (`GetMovies` is `movies`).
3. Put `[Authorize]` on every field except `version` (`optional-token-auth`).
4. Lists use `[UsePaging(ConnectionName = "...")]`, take `int? first` and `string? after`, and return `page.ToConnection()` with `after.ToOffset()` as the offset (`api-conventions`).
5. A mutation takes its input fields as plain arguments and returns the result itself. `AddMutationConventions(applyToAllMutations: true)` wraps them in `<Mutation>Input` and `<Mutation>Payload`. Add `[Error<ValidationError>]` and `[Error<NotFoundError>]` for the failures it can raise, and `[UseMutationConvention(PayloadFieldName = "...")]` when the result isn't an entity, such as `reviewId` on `deleteReview`.
6. A new entity gets a type in `Types/`: declare each field, mark non-null strings with `NonNullType<StringType>`, and resolve child lists on list items through a DataLoader.
7. Document the method (`<summary>`, `<param>` for each argument except `sender` and the token). The `<param>` tags become the input fields' descriptions.
8. Update the schema snapshot (`schema-snapshot`), then test the field through HTTP (`presentation-integration-tests`).

## Example

```csharp
// src/Presentation/GraphQL/Queries/MovieQueries.cs
/// <summary>
/// Lists movies ordered by title, one page at a time, optionally only those whose title contains a search term.
/// </summary>
/// <param name="query">The words to look for in the title, ignoring case. Leave empty to list every movie.</param>
[UsePaging(ConnectionName = "Movies")]
[Authorize]
public static async Task<Connection<MovieSummary>> GetMovies(
    string? query,
    int? first,
    string? after,
    ISender sender,
    CancellationToken cancellationToken)
{
    var page = await sender.Send(new Movies.GetMovies.GetMoviesQuery
    {
        Query = query,
        Offset = after.ToOffset(),
        Limit = first ?? Paging.DefaultLimit
    }, cancellationToken);

    return page.ToConnection();
}
```

```csharp
// src/Presentation/GraphQL/Mutations/ReviewMutations.cs
/// <summary>
/// Changes the star rating of a review and returns the updated review. The author and the movie of a review can't change.
/// </summary>
/// <param name="id">The unique identifier of the review.</param>
/// <param name="stars">The new star rating, from 1 to 5.</param>
[Authorize]
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
```

## Don't

- Register types one by one with `AddType` or `AddQueryType`. The generator does it.
- Use Hot Chocolate's `[UseOffsetPaging]`, `[UseFiltering]` or `[UseSorting]`, or return `IQueryable`. Paging, search and order belong to Application.
- Catch exceptions or check for null in a field.
- Write input or payload classes for a mutation, or return a `Boolean` to say it worked. The conventions build them, and the payload's `errors` says what went wrong.
- Resolve a child list per item with `sender.Send` inside a type. Use a DataLoader.
- Document `sender` or the cancellation token.

## Check

``` bash
dotnet build
dotnet test tests/Presentation.Tests.Integration
dotnet run --project ./src/Presentation   # then try it in Nitro at http://localhost:8080/graphql
```
