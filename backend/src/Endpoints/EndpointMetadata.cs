using RetailSystem.Api.Contracts;

namespace RetailSystem.Api.Endpoints;

internal static class EndpointMetadata
{
    public static RouteHandlerBuilder WithStandardErrors(this RouteHandlerBuilder builder) => builder
        .Produces<ApiEnvelope<object>>(StatusCodes.Status400BadRequest)
        .Produces<ApiEnvelope<object>>(StatusCodes.Status401Unauthorized)
        .Produces<ApiEnvelope<object>>(StatusCodes.Status403Forbidden)
        .Produces<ApiEnvelope<object>>(StatusCodes.Status404NotFound)
        .Produces<ApiEnvelope<object>>(StatusCodes.Status409Conflict)
        .Produces<ApiEnvelope<object>>(StatusCodes.Status500InternalServerError);
}
