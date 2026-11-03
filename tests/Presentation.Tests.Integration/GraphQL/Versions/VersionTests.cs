namespace CleanGraphQLApi.Presentation.Tests.Integration.GraphQL.Versions;

using System.Net;
using System.Threading.Tasks;
using Common;
using Shouldly;
using Xunit;

public class VersionTests : IDisposable
{
    private readonly GraphQLApplication application = new();

    [Fact]
    public async Task Version_ShouldReturn_Version()
    {
        // Arrange
        using var client = this.application.CreateClient();

        // Act
        using var response = await client.GetAsync("/graphql?query={version{fileVersion,informationalVersion}}");
        var result = (await response.Content.ReadAsStringAsync()).Deserialize<GraphData>();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        _ = result.ShouldNotBeNull();
        _ = result.Data.Version.ShouldNotBeNull();
        result.Data.Version.FileVersion.ShouldNotBeNullOrWhiteSpace();
        result.Data.Version.InformationalVersion.ShouldNotBeNullOrWhiteSpace();
    }

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
