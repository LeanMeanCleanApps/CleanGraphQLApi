namespace CleanGraphQLApi.Presentation.GraphQL.Queries;

#if includeAuth
using HotChocolate.Authorization;
#endif
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
#if includeAuth
    [Authorize]
#endif
    public static async Task<Entities.VersionInfo> GetVersion(ISender sender, CancellationToken cancellationToken)
    {
        return await sender.Send(new Versions.GetVersion.GetVersionQuery(), cancellationToken);
    }
}
