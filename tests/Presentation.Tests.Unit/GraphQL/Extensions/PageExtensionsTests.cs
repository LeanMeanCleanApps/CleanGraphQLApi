namespace CleanGraphQLApi.Presentation.Tests.Unit.GraphQL.Extensions;

using System.Text;
using Application.Common.Paging;
using FluentValidation;
using Presentation.GraphQL.Extensions;
using Shouldly;
using Xunit;

public class PageExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToOffset_ShouldReturn_Zero_WhenCursorIsEmpty(string? after)
    {
        // Act
        var result = after.ToOffset();

        // Assert
        result.ShouldBe(0);
    }

    [Fact]
    public void ToOffset_ShouldReturn_Offset_FromEndCursor()
    {
        // Arrange
        var connection = new Page<string>(["a", "b"], 10, 2, 30).ToConnection();

        // Act
        var result = connection.Info.EndCursor.ToOffset();

        // Assert
        result.ShouldBe(12);
    }

    [Theory]
    [InlineData("not base64!")]
    [InlineData("b2Zmc2V0Og==")]
    [InlineData("b2Zmc2V0Oi0x")]
    [InlineData("b2Zmc2V0OmFiYw==")]
    [InlineData("cGFnZToxMA==")]
    public void ToOffset_ShouldThrow_WhenCursorIsNotValid(string after)
    {
        // Act
        var exception = Should.Throw<ValidationException>(() => after.ToOffset());

        // Assert
        exception.Errors.ShouldContain(e => e.PropertyName == "After");
    }

    [Fact]
    public void ToConnection_ShouldReturn_EdgesAndPageInfo_ForAMiddlePage()
    {
        // Arrange
        var page = new Page<string>(["a", "b"], 10, 2, 30);

        // Act
        var result = page.ToConnection();

        // Assert
        result.TotalCount.ShouldBe(30);
        result.Edges.Select(e => e.Node).ShouldBe(["a", "b"]);
        result.Edges.Select(e => Decode(e.Cursor)).ShouldBe(["offset:11", "offset:12"]);
        result.Info.HasNextPage.ShouldBeTrue();
        result.Info.HasPreviousPage.ShouldBeTrue();
        result.Info.StartCursor.ShouldBe(result.Edges[0].Cursor);
        result.Info.EndCursor.ShouldBe(result.Edges[1].Cursor);
    }

    [Fact]
    public void ToConnection_ShouldReturn_NoNextPage_OnTheLastPage()
    {
        // Arrange
        var page = new Page<string>(["a"], 0, 10, 1);

        // Act
        var result = page.ToConnection();

        // Assert
        result.Info.HasNextPage.ShouldBeFalse();
        result.Info.HasPreviousPage.ShouldBeFalse();
    }

    [Fact]
    public void ToConnection_ShouldReturn_NoCursors_WhenPageIsEmpty()
    {
        // Arrange
        var page = new Page<string>([], 0, 10, 0);

        // Act
        var result = page.ToConnection();

        // Assert
        result.Edges.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
        result.Info.StartCursor.ShouldBeNull();
        result.Info.EndCursor.ShouldBeNull();
    }

    private static string Decode(string cursor)
    {
        return Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
    }
}
