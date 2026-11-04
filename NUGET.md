![Clean GraphQL API](https://raw.githubusercontent.com/LeanMeanCleanApps/brand/main/assets/banners/png/graphql.png)

A `dotnet new` template for a GraphQL API using [Hot Chocolate](https://chillicream.com/docs/hotchocolate) in .NET 10, built with a streamlined Clean Architecture. All the layers you need, none of the ceremony you don't.

It's one of five [Lean Mean Clean Apps](https://leanmeancleanapps.github.io/) templates, for Minimal APIs, Model Context Protocol, GraphQL, Web API controllers and AWS Lambda. They share the same Application and Infrastructure layers and differ only in how they face the world. The full docs live at [leanmeancleanapps.github.io/graphql](https://leanmeancleanapps.github.io/graphql/).

## Get started

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). [Docker](https://www.docker.com/products/docker-desktop) is optional, but it's the quickest way to see it running.

```bash
# Install the template
dotnet new install CleanGraphQLApi.Template

# Create a solution
dotnet new cleangraphqlapi --name Acme.Reviews --authorName "Your Name"
cd Acme.Reviews
```

| Option | What it does |
| --- | --- |
| `--name` | Names the solution and its root namespace |
| `--authorName` | Puts your name on the assembly instead of ours |
| `--authority` | Your OAuth authorization server. Leave it out and the API runs without authentication |
| `--audience` | The audience tokens must be issued for, usually the API's own URL |

## Run it

```bash
# Build the image and start it with sample data, plus a collector and the Aspire dashboard
docker compose up -d --build

# Run the tests while you're here
dotnet test
```

Then open [Nitro](https://chillicream.com/docs/nitro), the GraphQL IDE, at <http://localhost:8080/graphql> and ask it for some movies.

The API is open until you pass `--authority` and `--audience`, and then the queries and mutations ask for a JWT bearer token.

## What's inside

- **Presentation**: the GraphQL schema, built by [Hot Chocolate](https://chillicream.com/docs/hotchocolate) with cursor paging, DataLoaders and the Nitro IDE
- **Application**: commands and queries through [Mediator](https://github.com/martinothamar/Mediator), validated by [FluentValidation](https://github.com/FluentValidation/FluentValidation) and mapped by [Mapperly](https://mapperly.riok.app)
- **Infrastructure**: [Entity Framework Core](https://learn.microsoft.com/ef/core) on SQLite, with optional sample data
- **Tests**: [xUnit](https://xunit.net), [Shouldly](https://github.com/shouldly/shouldly) and [NSubstitute](https://github.com/nsubstitute/NSubstitute), each on its own throwaway database
- **Extras**: [OpenTelemetry](https://opentelemetry.io) traces, metrics and [Serilog](https://github.com/serilog/serilog) logs over OTLP, health checks, a Dockerfile that runs as a non-root user, Docker Compose, and a GitHub Actions pipeline that won't let your coverage slip

## Releases

A new version lands on the 1st of each month, as long as something changed. No changes, no release. We're not here to pad the download stats.

```bash
dotnet new update
```

## More help

- [Docs](https://leanmeancleanapps.github.io/graphql/) for guides, a reference and why it's built this way
- [GitHub](https://github.com/LeanMeanCleanApps/CleanGraphQLApi) for the code, [issues](https://github.com/LeanMeanCleanApps/CleanGraphQLApi/issues) and [release notes](https://github.com/LeanMeanCleanApps/CleanGraphQLApi/releases)
- [Security policy](https://github.com/LeanMeanCleanApps/CleanGraphQLApi/security/policy) to report a vulnerability privately

## Who built this

Built by [Stephen Walsh](https://github.com/stphnwlsh) in spare time, with strong coffee and a healthy disregard for pomp and ceremony. If this template saves you some time, [sponsoring the work](https://github.com/sponsors/stphnwlsh) keeps all five templates current and the coffee flowing.
