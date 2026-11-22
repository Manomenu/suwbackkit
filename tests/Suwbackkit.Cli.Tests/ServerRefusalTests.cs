namespace Suwbackkit.Cli.Tests;

public sealed class ServerRefusalTests
{
    [Fact]
    public void A_refusal_gives_its_code_title_detail_and_every_mistake_with_its_fix_and_place()
    {
        var refusal = ServerRefusal.Read(
            """
            {"type":"about:blank","title":"The request is not valid","status":422,"detail":"2 mistakes in `mistakes`.","traceId":"t",
             "code":"request.invalid","mistakes":[
               {"code":"field.invalid","message":"The page is below 1.","fix":"Ask for page 1 or later.","at":"page"},
               {"code":"reference.missing","message":"No article a-9.","fix":"Create it.","related":[{"resource":{"kind":"Article","id":"a-9"}}]}]}
            """
        );

        Assert.NotNull(refusal);
        Assert.Equal(
            ("request.invalid", "The request is not valid", "2 mistakes in `mistakes`."),
            (refusal.Code, refusal.Title, refusal.Detail)
        );
        Assert.Equal(
            [
                ("field.invalid", "The page is below 1.", "Ask for page 1 or later.", "page"),
                ("reference.missing", "No article a-9.", "Create it.", null),
            ],
            refusal.Mistakes.Select(mistake => (mistake.Code, mistake.Message, mistake.Fix, mistake.At))
        );
        Assert.Equal("a-9", refusal.Mistakes[1].Json.GetProperty("related")[0].GetProperty("resource").GetProperty("id").GetString());
    }

    [Fact]
    public void A_refusal_without_mistakes_has_an_empty_list()
    {
        var refusal = ServerRefusal.Read("""{"title":"Feature off","detail":"Monitoring is off.","code":"monitoring.off"}""");

        Assert.NotNull(refusal);
        Assert.Empty(refusal.Mistakes);
    }

    [Fact]
    public void A_field_that_is_not_text_reads_as_missing()
    {
        var refusal = ServerRefusal.Read("""{"code":7,"mistakes":[{"message":["a"],"fix":null},"plain"]}""");

        Assert.NotNull(refusal);
        Assert.Null(refusal.Code);
        Assert.All(refusal.Mistakes, mistake =>
            Assert.Equal((null, null, null, null), (mistake.Code, mistake.Message, mistake.Fix, mistake.At))
        );
    }

    [Theory]
    [InlineData("<html>busy</html>")]
    [InlineData("")]
    [InlineData("[1,2]")]
    [InlineData("\"text\"")]
    public void An_answer_that_is_no_json_object_is_no_refusal(string body) => Assert.Null(ServerRefusal.Read(body));
}
