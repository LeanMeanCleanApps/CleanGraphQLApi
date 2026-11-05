---
name: schema-snapshot
description: How the GraphQL schema is pinned by a snapshot test - schema.graphql in the integration tests, comparing it with /graphql?sdl, and updating it on purpose when the schema changes. Use when the schema snapshot test fails, or after adding or changing a query, mutation, type, field or description.
metadata:
  scope: presentation
---

# Schema snapshot

## The rule

`tests/Presentation.Tests.Integration/GraphQL/Schema/schema.graphql` is the schema clients see, checked in. `SchemaTests.Schema_ShouldMatch_Snapshot` asks the running app for `/graphql?sdl` and fails when the two differ. Every schema change shows up in a pull request as a diff of that file, so reviewers see exactly what clients will see.

The test ignores lines that start with `#`, which GraphQL treats as comments. In the template repository the snapshot uses them to mark the `ApplyPolicy` enum, which only exists when authorization is on, so `dotnet new` can leave it out of a solution created without `--auth`. Keep those markers when you update the snapshot there.

## Steps

1. Make the schema change.
2. Run the tests. The snapshot test fails and prints the new schema.
3. Copy the new SDL into `schema.graphql`. Either take it from the test output, or run the app and open <http://localhost:8080/graphql?sdl>.
4. Read the diff. A removed field, a renamed one, or a field that became non-null breaks clients, so say so in the pull request.
5. Run the tests again.

## Don't

- Update the snapshot without reading the diff.
- Edit `schema.graphql` by hand to make the test pass.
- Skip or delete the snapshot test.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration --filter "FullyQualifiedName~SchemaTests"
git diff tests/Presentation.Tests.Integration/GraphQL/Schema/schema.graphql
```
