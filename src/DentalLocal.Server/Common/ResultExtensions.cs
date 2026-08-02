using DentalLocal.Application.Common;
using Microsoft.AspNetCore.Http;

namespace DentalLocal.Server.Common;

/// <summary>Маппинг Result/Result&lt;T&gt; в HTTP-ответ Minimal API (успех → JSON, ошибка → ProblemDetails RFC 7807).</summary>
public static class ResultExtensions
{
    public static IResult ToApiResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result.Error);

    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> locationFactory) =>
        result.IsSuccess ? Results.Created(locationFactory(result.Value), result.Value) : ToProblem(result.Error);

    /// <summary>202 Accepted для команд, которые ставят долгую операцию в job-очередь вместо
    /// выполнения внутри HTTP-запроса (см. Application/Common/Jobs.cs).</summary>
    public static IResult ToAcceptedResult<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Accepted(value: result.Value) : ToProblem(result.Error);

    public static IResult ToApiResult(this Result result) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result.Error);

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = statusCode,
            Title = error.Message,
            Extensions = { ["code"] = error.Code },
        };

        if (error.FieldErrors is { Count: > 0 })
            problem.Extensions["fieldErrors"] = error.FieldErrors;

        return Results.Problem(
            statusCode: statusCode,
            title: problem.Title,
            extensions: problem.Extensions);
    }
}
