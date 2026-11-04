---
name: nitro
description: How to try the GraphQL API by hand in Nitro, the GraphQL IDE served embedded at /graphql - where it runs, sending a token when authentication is on, and what to check before calling a schema change done. Use when you want to explore the schema, run a query by hand, or see the descriptions clients see.
metadata:
  scope: presentation
---

# Nitro

## The rule

`MapGraphQL()` serves Nitro, Hot Chocolate's GraphQL IDE, at <http://localhost:8080/graphql> when a browser asks for it. It's served embedded (`ServeMode.Embedded`) so it works offline and never loads from a CDN. The schema and Nitro stay open when authentication is on; every field, `version` included, asks for a token.

| What | Address |
| --- | --- |
| Nitro | <http://localhost:8080/graphql> |
| Schema as SDL | <http://localhost:8080/graphql?sdl> |
| Aspire dashboard (logs, traces and metrics, in Docker) | <http://localhost:18888> |

## Steps

1. Run the app (`dotnet run --project ./src/Presentation`) or the compose file, and open Nitro.
2. Check the new field's description, arguments and nullability in the schema reference. They come from XML comments (`xml-docs-and-openapi`).
3. Run the happy path and the failure paths, and check the `extensions.code` of each error.
4. With authentication on, add an `Authorization: Bearer <token>` header in Nitro's connection settings.
5. Write the integration test as well (`presentation-integration-tests`).

## Don't

- Switch Nitro back to CDN mode. It fails offline and in locked-down networks.
- Treat a manual check as a test.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration --filter "FullyQualifiedName~Nitro_ShouldBe_Served"
```
