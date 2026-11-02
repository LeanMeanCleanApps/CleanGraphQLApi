---
name: presentation-integration-tests
description: How this GraphQL API is tested end to end with WebApplicationFactory - the GraphQLApplication host, one app and database per test class, sending queries and mutations over HTTP, reading data and errors with typed test models, the schema snapshot, and testing with authentication on. Use when writing or fixing a test in tests/Presentation.Tests.Integration.
metadata:
  scope: presentation
---

# Presentation integration tests (GraphQL)

## The rule

The GraphQL layer is tested only through HTTP, in `tests/Presentation.Tests.Integration`. `GraphQLApplication` (a `WebApplicationFactory<Program>`) starts the app with the `local` environment, its own shared-cache in-memory SQLite database and the sample data on. Each test class creates one in a field, implements `IDisposable` and disposes it, so no class shares data with another.

| Piece | Where | Use it to |
| --- | --- | --- |
| `GraphQLApplication(environment, settings, configureServices)` | `GraphQLApplication.cs` | Start the app, with other settings or services when needed |
| `Deserialize<T>()`, `ToStringContent()` | `Serializer.cs` | Read a response into a test model, and send a query with variables as JSON |
| `GraphData`, `Data`, `Error`, `Connection<T>` | `GraphQL/Common/` | The shape of a GraphQL response: `data` and `errors` |
| Per-feature models | `GraphQL/<Feature>/` | The fields a test asks for |

## Steps

1. Put the test in `GraphQL/<Feature>/<Feature>Tests.cs` with `private readonly GraphQLApplication application = new();` and the `Dispose` pattern from the other classes.
2. Queries can go in the URL (`/graphql?query={...}`). Mutations and anything with variables are POSTed as `{ query, variables }` with `ToStringContent()`.
3. A GraphQL response is `200 OK` even when it has errors. Assert the status, then `errors` or `data`.
4. For failures, assert `errors[0].extensions.code` (`VALIDATION`, `NOT_FOUND`) and the messages (`api-conventions`).
5. Find rows through the API (list, then pick one), not by hard-coded ids.
6. A schema change also updates the snapshot (`schema-snapshot`).
7. Authentication tests pass `Authentication:*` settings and a test signing key. See `Authentication/AuthenticationTests.cs`.

## Example

```csharp
// tests/Presentation.Tests.Integration/GraphQL/Movies/MovieTests.cs
public class MovieTests : IDisposable
{
    private readonly GraphQLApplication application = new();

    [Fact]
    public async Task Movies_ShouldReturn_MoviesList()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={movies(first:50){nodes{id,title,reviewCount,averageStars,reviews{id,stars,author{id,firstName,lastName}}}}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Data.Movies.Nodes.Length.ShouldBe(50);
    }
}
```

## Don't

- Share an application across tests with a `static` field or a class fixture.
- Assert only the status code. It's 200 for errors too.
- Edit appsettings files for a test. Pass `settings`.
- Depend on order the query doesn't promise.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration
```
