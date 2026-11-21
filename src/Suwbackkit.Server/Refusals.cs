using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Suwbackkit.Server;

public static class Refusals
{
    public const string ContentType = "application/problem+json";

    public static ProblemHttpResult Refuse<TCode>(int status, string title, TCode code, string detail)
        where TCode : notnull
    {
        Require(title, nameof(title));
        Require(detail, nameof(detail));
        return Problem(status, title, detail, Extensions(code));
    }

    public static ProblemHttpResult Refuse<TCode, TMistakeCode>(
        int status,
        string title,
        TCode code,
        IReadOnlyList<Mistake<TMistakeCode>> mistakes
    )
        where TCode : notnull
        where TMistakeCode : notnull
    {
        Require(title, nameof(title));
        ArgumentNullException.ThrowIfNull(mistakes);
        if (mistakes.Count == 0)
        {
            throw new ArgumentException(
                "A refusal with no mistakes says what to do in its detail: call Refuse(status, title, code, detail).",
                nameof(mistakes)
            );
        }
        foreach (var mistake in mistakes)
        {
            RequireCode(mistake.Code, nameof(mistakes));
            Require(mistake.Message, nameof(mistakes));
            Require(mistake.Fix, nameof(mistakes));
        }
        var extensions = Extensions(code);
        extensions["mistakes"] = mistakes;
        return Problem(status, title, DetailOf(mistakes.Count), extensions);
    }

    public static RouteHandlerBuilder ProducesRefusal<TCode>(this RouteHandlerBuilder builder, int status)
        where TCode : notnull => builder.ProducesRefusal<TCode, Mistake<TCode>>(status);

    public static RouteHandlerBuilder ProducesRefusal<TCode, TMistake>(this RouteHandlerBuilder builder, int status)
        where TCode : notnull
        where TMistake : Mistake<TCode> => builder.ProducesRefusal<TCode, TCode, TMistake>(status);

    public static RouteHandlerBuilder ProducesRefusal<TCode, TMistakeCode, TMistake>(this RouteHandlerBuilder builder, int status)
        where TCode : notnull
        where TMistakeCode : notnull
        where TMistake : Mistake<TMistakeCode> =>
        builder.WithMetadata(new ProducesResponseTypeMetadata(status, typeof(Refusal<TCode, TMistake>), [ContentType]));

    internal static string DetailOf(int mistakes) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{mistakes} {(mistakes == 1 ? "mistake" : "mistakes")} in `mistakes`: fix each as its `fix` says, then try again."
        );

    private static Dictionary<string, object?> Extensions<TCode>(TCode code)
        where TCode : notnull
    {
        RequireCode(code, nameof(code));
        return new Dictionary<string, object?> { ["code"] = code };
    }

    private static ProblemHttpResult Problem(int status, string title, string detail, Dictionary<string, object?> extensions) =>
        TypedResults.Problem(title: title, detail: detail, statusCode: status, extensions: extensions);

    private static void RequireCode<TCode>(TCode code, string parameter)
    {
        if (code is null || (code is string text && string.IsNullOrWhiteSpace(text)))
        {
            throw new ArgumentException("A refusal and each of its mistakes need a code that is not empty.", parameter);
        }
    }

    private static void Require(string? text, string parameter)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "A refusal needs a title and a detail, and each mistake a message and a fix, none of them empty.",
                parameter
            );
        }
    }
}
