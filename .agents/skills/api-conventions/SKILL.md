---
name: api-conventions
description: How this GraphQL schema is named and shaped - camelCase fields, nullability, Relay connections over offset paging with opaque cursors, the VALIDATION and NOT_FOUND error codes, depth limits and authorization. Use when naming a field or argument, deciding nullability, returning a list, or changing how errors appear.
metadata:
  scope: presentation
---

# API conventions (GraphQL)

## The rule

| Thing | Convention |
| --- | --- |
| Fields and arguments | camelCase, named for what they return: `movie(id)`, `movies(query, first, after)` |
| Types | The Application entity's name: `Movie`, `MovieSummary`, `Review` |
| Inputs | `<Verb><Thing>Input`: `CreateReviewInput` |
| Ids | `UUID` |
| Nullability | Entity fields are non-null. A field that can fail returns a nullable result, so an error nulls only that field |
| Lists | Relay connections (`nodes`, `edges`, `pageInfo`, `totalCount`), forward only, `first` defaults to 10 and is at most 50 |
| Depth | Queries deeper than 15 levels are refused (`AddMaxExecutionDepthRule(15)`) |
| Cost | Hot Chocolate's cost analysis is on, so the schema carries `@cost` and `@listSize` and expensive queries are refused |

Connections sit on top of the shared offset `paging`. `PageExtensions` in `GraphQL/Extensions/` makes each cursor a base64 `offset:<n>` and turns an `after` cursor back into an offset. A cursor that didn't come from us is a validation error.

`ApplicationErrorFilter` in `GraphQL/Errors/` maps Application exceptions onto GraphQL errors:

| Exception | `extensions.code` | Message and extensions |
| --- | --- | --- |
| `ValidationException` | `VALIDATION` | "One or more validation errors occurred.", with `extensions.errors` grouping messages by property |
| `NotFoundException` | `NOT_FOUND` | The exception's message |
| Anything else | Hot Chocolate's own | "Unexpected Execution Error", with the exception details hidden unless a debugger is attached |

## Steps

1. Name new fields and arguments after the data, not the Application class (`reviews`, not `getReviewsQuery`).
2. Decide nullability on purpose: non-null for fields that always have a value, nullable for a field whose resolver can throw.
3. Return a connection for any list that can grow. Never an unbounded list.
4. Report a new kind of failure as an Application exception and add a case to `ApplicationErrorFilter` with its own code, with a unit test in `tests/Presentation.Tests.Unit/GraphQL/Errors/`.
5. Test each error code in the field's integration tests (`test-design`).

## Example

```csharp
// src/Presentation/GraphQL/Errors/ApplicationErrorFilter.cs
NotFoundException notFoundException => ErrorBuilder.FromError(error)
    .SetMessage(notFoundException.Message)
    .SetCode("NOT_FOUND")
    .Build(),
```

## Don't

- Expose offset arguments or backward paging.
- Change a field's name, type or nullability without updating the schema snapshot and saying so in the pull request. It breaks clients.
- Put stack traces or internal detail in an error message or extension.
- Add a second error filter.

## Check

``` bash
dotnet test tests/Presentation.Tests.Integration
```
