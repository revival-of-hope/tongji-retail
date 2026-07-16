namespace RetailSystem.Api.Contracts;

public sealed record ApiEnvelope<T>(int Code, string Message, T? Data);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int PageIndex,
    int PageSize,
    int TotalCount,
    int TotalPages);

public static class ApiResults
{
    public static IResult Ok<T>(T data, string message = "success") =>
        Results.Ok(new ApiEnvelope<T>(StatusCodes.Status200OK, message, data));

    public static IResult Created<T>(string location, T data, string message = "created") =>
        Results.Created(location, new ApiEnvelope<T>(StatusCodes.Status201Created, message, data));

    public static IResult BadRequest(string message) =>
        Results.BadRequest(new ApiEnvelope<object>(StatusCodes.Status400BadRequest, message, null));

    public static IResult Unauthorized(string message = "unauthorized") =>
        Results.Json(new ApiEnvelope<object>(StatusCodes.Status401Unauthorized, message, null), statusCode: StatusCodes.Status401Unauthorized);

    public static IResult Forbidden(string message = "forbidden") =>
        Results.Json(new ApiEnvelope<object>(StatusCodes.Status403Forbidden, message, null), statusCode: StatusCodes.Status403Forbidden);

    public static IResult NotFound(string message = "not found") =>
        Results.NotFound(new ApiEnvelope<object>(StatusCodes.Status404NotFound, message, null));

    public static IResult Conflict(string message) =>
        Results.Conflict(new ApiEnvelope<object>(StatusCodes.Status409Conflict, message, null));
}
