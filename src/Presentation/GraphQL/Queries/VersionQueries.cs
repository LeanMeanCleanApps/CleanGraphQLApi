namespace CleanGraphQLApi.Presentation.GraphQL.Queries;

using HotChocolate.Authorization;
using HotChocolate.Types;
using Mediator;
using Entities = Application.Versions.Entities;
using Versions = Application.Versions.Queries;

[QueryType]
public static partial class VersionQueries
{
    /// <summary>
    /// Gets the file and informational versions of the running application.
    /// </summary>
    [Authorize]
    public static async Task<Entities.VersionInfo> GetVersion(ISender sender, CancellationToken cancellationToken)
    {
        return await sender.Send(new Versions.GetVersion.GetVersionQuery(), cancellationToken);
    }
}
