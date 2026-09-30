using System.Net;
using Microsoft.AspNetCore.Mvc;
using SpotifyAPI.Web;

namespace ConflictedLineup.Api.Controllers;

/// <summary>
/// Turns Spotify failures into messages a user can act on, without echoing Spotify's raw error text.
/// </summary>
internal static class SpotifyErrors
{
    public static string Describe(Exception ex, string fallback) => ex switch
    {
        APIUnauthorizedException => "Your Spotify session has expired. Log in again.",
        APITooManyRequestsException => "Spotify is rate limiting requests right now. Try again in a few minutes.",
        APIException { Response.StatusCode: HttpStatusCode.Forbidden } =>
            "Spotify refused the request. This app runs in Spotify's development mode, which only allows accounts on its user list.",
        _ => fallback
    };

    public static ObjectResult ToResult(Exception ex, string fallback)
    {
        var status = ex switch
        {
            APIUnauthorizedException => StatusCodes.Status401Unauthorized,
            APITooManyRequestsException => StatusCodes.Status503ServiceUnavailable,
            APIException => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        };

        return new ObjectResult(new { error = Describe(ex, fallback) }) { StatusCode = status };
    }
}
