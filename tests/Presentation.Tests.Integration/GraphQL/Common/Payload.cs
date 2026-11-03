namespace CleanGraphQLApi.Presentation.Tests.Integration.GraphQL.Common;

using System.Text.Json.Serialization;
using Reviews.Query;

public partial class ReviewPayload
{
    [JsonPropertyName("review")]
    public Review? Review { get; set; }

    [JsonPropertyName("errors")]
    public List<MutationError>? Errors { get; set; }
}

public partial class DeleteReviewPayload
{
    [JsonPropertyName("reviewId")]
    public Guid? ReviewId { get; set; }

    [JsonPropertyName("errors")]
    public List<MutationError>? Errors { get; set; }
}

public partial class MutationError
{
    [JsonPropertyName("__typename")]
    public string TypeName { get; set; } = null!;

    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    [JsonPropertyName("fields")]
    public List<InvalidField>? Fields { get; set; }
}

public partial class InvalidField
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = null!;

    [JsonPropertyName("messages")]
    public List<string> Messages { get; set; } = null!;
}
