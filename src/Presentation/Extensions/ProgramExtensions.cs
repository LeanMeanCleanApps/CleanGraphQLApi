namespace CleanGraphQLApi.Presentation.Extensions;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Application;
using Application.Common.Paging;
using Application.Common.Telemetry;
using Application.Versions.Entities;
#if auth
using Authentication;
#endif
using ChilliCream.Nitro.App;
using GraphQL.Errors;
using Infrastructure;
#if auth
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
#endif
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Telemetry;

[ExcludeFromCodeCoverage]
public static class ProgramExtensions
{
    public static WebApplicationBuilder ConfigureBuilder(this WebApplicationBuilder builder)
    {
        #region Telemetry

        var assembly = typeof(ProgramExtensions).Assembly;
        var telemetry = TelemetrySettings.FromConfiguration(builder.Configuration, builder.Environment.EnvironmentName, assembly);

        var openTelemetry = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddAttributes(telemetry.ResourceAttributes))
            .WithTracing(tracing => tracing
                .AddSource(ApplicationTelemetry.Name)
                .AddAspNetCoreInstrumentation(options =>
                {
                    // Probes call the health endpoints every few seconds, so tracing them would bury everything else.
                    options.Filter = context => !context.Request.Path.StartsWithSegments("/health");
                    options.RecordException = true;
                })
                .AddHttpClientInstrumentation(options => options.RecordException = true)
                .AddHotChocolateInstrumentation())
            .WithMetrics(metrics => metrics
#if auth
                .AddMeter(
                    "Microsoft.EntityFrameworkCore",
                    "Microsoft.AspNetCore.Authentication",
                    "Microsoft.AspNetCore.Authorization")
#else
                .AddMeter("Microsoft.EntityFrameworkCore")
#endif
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .SetExemplarFilter(ExemplarFilterType.TraceBased));

        // The exporters read OTEL_EXPORTER_OTLP_ENDPOINT and the other OTEL_* settings themselves.
        if (telemetry.Enabled)
        {
            _ = openTelemetry.UseOtlpExporter();
        }

        #endregion Telemetry

        #region Logging

        _ = builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            var configuration = services.GetRequiredService<IConfiguration>();

            _ = loggerConfiguration.ReadFrom.Configuration(configuration)
                .Enrich.WithProperty(
                    "AssemblyVersion",
                    assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version)
                .Enrich.WithProperty(
                    "AssemblyInformationalVersion",
                    assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);

            // Logs leave over OTLP too, carrying the trace and span IDs of whatever was running when they were written.
            if (telemetry.Enabled)
            {
                _ = loggerConfiguration.WriteTo.OpenTelemetry(
                    options =>
                    {
                        options.ResourceAttributes = telemetry.ResourceAttributes;
                        options.OnBeginSuppressInstrumentation = SuppressInstrumentationScope.Begin;
                    },
                    key => configuration[key]);
            }
        });

        #endregion Logging

#if auth
        #region Authentication

        var authentication = AuthenticationSettings.FromConfiguration(builder.Configuration);

        if (authentication.Enabled)
        {
            _ = builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authentication.Authority;
                    options.Audience = authentication.Audience;
                    options.MapInboundClaims = false;
                });
            _ = builder.Services.AddAuthorization();
        }
        else
        {
            // Fields always ask for authorization, so without an authority every request is let through.
            _ = builder.Services.AddAuthorizationBuilder()
                .SetDefaultPolicy(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());
        }

        #endregion Authentication

#endif
        #region GraphQL

        _ = builder.Services
            .AddGraphQLServer()
            .AddPresentationTypes()
#if auth
            .AddAuthorization()
#endif
            .AddInstrumentation()
            .AddMutationConventions(applyToAllMutations: true)
            // The filter is built from the schema's own services, so its logger comes from the application's.
            .AddErrorFilter(services => new ApplicationErrorFilter(
                services.GetRootServiceProvider().GetRequiredService<ILogger<ApplicationErrorFilter>>()))
            .AddMaxExecutionDepthRule(15)
            .ModifyServerOptions(options => options.Tool.ServeMode = ServeMode.Embedded)
            .ModifyPagingOptions(options =>
            {
                options.DefaultPageSize = Paging.DefaultLimit;
                options.MaxPageSize = Paging.MaxLimit;
                options.IncludeTotalCount = true;
                options.AllowBackwardPagination = false;
            });

        #endregion GraphQL

        #region Project Dependencies

        _ = builder.Services.AddMediator(options => options.ServiceLifetime = ServiceLifetime.Scoped);
        _ = builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("MovieReviews"));
        _ = builder.Services.AddApplication();
        _ = builder.Services.AddSingleton(VersionInfo.FromAssembly(typeof(ProgramExtensions).Assembly));
        _ = builder.Services.AddHealthChecks().AddInfrastructureHealthChecks("ready");

        #endregion Project Dependencies

        return builder;
    }

    public static WebApplication ConfigureApplication(this WebApplication app)
    {
        #region Database

        _ = app.Services.InitialiseInfrastructure(app.Configuration.GetValue<bool>("Database:SeedSampleData"));

        #endregion Database

        #region Security

        // HSTS tells browsers to only use HTTPS from then on, so it's left off for local runs.
        if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("local"))
        {
            _ = app.UseHsts();
        }

        _ = app.UseHttpsRedirection();

        #endregion Security

        #region Logging

        _ = app.UseSerilogRequestLogging();

        #endregion Logging

#if auth
        #region Authentication

        if (AuthenticationSettings.FromConfiguration(app.Configuration).Enabled)
        {
            _ = app.UseAuthentication();
        }

        _ = app.UseAuthorization();

        #endregion Authentication

#endif
        #region Health

        // Liveness runs no checks, so it only fails when the process can't answer. Readiness checks the database.
#if auth
        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false }).AllowAnonymous();
        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") }).AllowAnonymous();
#else
        _ = app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        _ = app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });
#endif

        #endregion Health

        #region GraphQL

        _ = app.MapGraphQL();

        #endregion GraphQL

        return app;
    }
}
