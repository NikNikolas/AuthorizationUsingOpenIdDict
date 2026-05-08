using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using System.Security.Claims;

namespace AuthServer.Services
{
    public class AuthService
    {
        public bool IsAuthenticated(AuthenticateResult result, OpenIddictRequest request)
        {

            if (!result.Succeeded)
            {
                return false;
            }

            if (request.MaxAge.HasValue && result.Properties != null)
            {
                var maxAgeSeconds = TimeSpan.FromSeconds(request.MaxAge.Value);

                var expired = !result.Properties.IssuedUtc.HasValue ||
                              result.Properties.IssuedUtc + maxAgeSeconds <= DateTimeOffset.UtcNow;
                if (expired)
                {
                    return false;
                }
            }

            return true;
        }

        public IDictionary<string, StringValues> ParseOAuthParameters(HttpContext httpContext, List<string>? excluding = null)
        {
            excluding ??= new List<string>();

            var parameters = httpContext.Request.HasFormContentType
                ? httpContext.Request.Form
                    .Where(v => !excluding.Contains(v.Key))
                    .ToDictionary(v => v.Key, v => v.Value)
                : httpContext.Request.Query
                    .Where(v => !excluding.Contains(v.Key))
                    .ToDictionary(v => v.Key, v => v.Value);

            return parameters;
        }

        public string BuildRedirectUrl(HttpRequest request, IDictionary<string, StringValues> oAuthParameters)
        {
            var url = request.PathBase + request.Path + QueryString.Create(oAuthParameters);
            //var url = request.Host + request.Path + QueryString.Create(oAuthParameters);
            return url;
        }

        public static List<string> GetDestinations(ClaimsIdentity identity, Claim claim)
        {
            var destinations = new List<string>();

            if (claim.Type is OpenIddictConstants.Claims.Name or OpenIddictConstants.Claims.Email)
            {
                destinations.Add(OpenIddictConstants.Destinations.AccessToken);
            }

            return destinations;
        }
    }
}
