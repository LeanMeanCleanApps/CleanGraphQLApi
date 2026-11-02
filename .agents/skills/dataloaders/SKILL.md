---
name: dataloaders
description: How Hot Chocolate DataLoaders batch the reviews asked for by every movie or author in a response into one Application query - source-generated DataLoaders, batching under the paging limit, and using them from a type. Use when a field on a list item needs data from another query, or when a query makes one database call per item.
metadata:
  scope: presentation
---

# DataLoaders

## The rule

A field on a list item never sends its own query. Asking for the reviews of 50 movies must cost one Application query, not 50. DataLoaders in `src/Presentation/GraphQL/DataLoaders/` collect the ids asked for during one request and load them together.

They're static methods with `[DataLoader]`. The source generator creates an interface and class for each one: `GetReviewsByMovieIdAsync` becomes `IReviewsByMovieIdDataLoader`. Each takes the ids, then `ISender` and the token, and returns a `Dictionary<id, value>` with an entry for every id, empty when there's nothing.

Batches are split into chunks of `Paging.MaxLimit`, because the Application queries refuse more ids than that.

## Steps

1. Add an Application query that takes a list of ids and returns a dictionary (`add-a-use-case`), like `GetReviewsForMoviesQuery`.
2. Add a `[DataLoader]` method that chunks the ids by `Paging.MaxLimit` and sends the query for each chunk.
3. Return an entry for every id asked for, so callers can use `LoadRequiredAsync`.
4. In the type, add a resolver that takes `[Parent]`, the generated DataLoader interface and the token, and calls `LoadRequiredAsync(parent.Id, cancellationToken)`.
5. Test a list query that asks for the child field, and check the counts match (`presentation-integration-tests`).

## Example

```csharp
// src/Presentation/GraphQL/Types/MovieSummaryType.cs
public static async Task<List<Review>> GetReviewsAsync(
    [Parent] MovieSummary movie,
    IReviewsByMovieIdDataLoader reviews,
    CancellationToken cancellationToken)
{
    return await reviews.LoadRequiredAsync(movie.Id, cancellationToken);
}
```

## Don't

- Call `sender.Send` from a resolver on a list item.
- Return a dictionary missing some ids.
- Send more than `Paging.MaxLimit` ids in one Application query.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration --filter "FullyQualifiedName~Movies|FullyQualifiedName~Authors"
```
