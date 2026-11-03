namespace CleanGraphQLApi.Presentation.GraphQL.Errors;

using FluentValidation;

/// <summary>
/// The input broke one or more rules, so nothing changed.
/// </summary>
/// <param name="exception">The exception the Application layer raised.</param>
public sealed class ValidationError(ValidationException exception)
{
    /// <summary>
    /// A summary of the problem.
    /// </summary>
    public string Message => "One or more validation errors occurred.";

    /// <summary>
    /// Each input field that broke a rule, with what was wrong with it.
    /// </summary>
    public IReadOnlyList<InvalidField> Fields { get; } = [.. exception.Errors
        .GroupBy(e => e.PropertyName)
        .Select(g => new InvalidField(g.Key, [.. g.Select(e => e.ErrorMessage).Distinct()]))];
}

/// <summary>
/// One input field that broke a rule.
/// </summary>
/// <param name="Field">The name of the field, as the Application request names it.</param>
/// <param name="Messages">What was wrong with it.</param>
public sealed record InvalidField(string Field, IReadOnlyList<string> Messages);
