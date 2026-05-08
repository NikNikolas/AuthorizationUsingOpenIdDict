using AuthServer.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Web;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer.Controllers
{
    [ApiController]
    public class AuthorizationController : Controller
    {
        private readonly IOpenIddictApplicationManager _applicationManager;
        private readonly IOpenIddictAuthorizationManager _authorizationManager;
        private readonly IOpenIddictScopeManager _scopeManager;
        private readonly AuthService _authService;

        public AuthorizationController(
           IOpenIddictApplicationManager applicationManager,
           IOpenIddictAuthorizationManager authorizationManager,
           IOpenIddictScopeManager scopeManager,
           AuthService authService)
        {
            _applicationManager = applicationManager;
            _authorizationManager = authorizationManager;
            _scopeManager = scopeManager;
            _authService = authService;
        }


        [HttpGet("~/connect/authorize")]
        [HttpPost("~/connect/authorize")]
        public async Task<IActionResult> Authorize()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request is missing.");

            var parameters = _authService.ParseOAuthParameters(HttpContext, new List<string> { Parameters.Prompt });

            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var isAutheniticated = _authService.IsAuthenticated(result, request);


            if (!isAutheniticated)
            {
                //return Redirect("/Authenticate");

                var redirectUrl = _authService.BuildRedirectUrl(HttpContext.Request, parameters);

                //return Redirect($"/Authenticate?ReturnUrl={Uri.EscapeDataString(redirectUrl)}");

                return Challenge(properties: new AuthenticationProperties
                {
                    RedirectUri = redirectUrl
                }, new[] { CookieAuthenticationDefaults.AuthenticationScheme });
            }

            var clientApp = await _applicationManager.FindByClientIdAsync(request.ClientId) ??
                throw new InvalidOperationException("Calling client application cannot be found.");

            var consentClaim = result.Principal.GetClaim(Consts.ConsentNaming);

            if (consentClaim != Consts.GrantAccessValue) //TODO user can get into loop with Deny consent option?
            {
                var returnUrl = HttpUtility.UrlEncode(_authService.BuildRedirectUrl(HttpContext.Request, parameters));
                var consentRedirectUrl = $"/Consent?returnUrl={returnUrl}";

                return Redirect(consentRedirectUrl);
            }

            var userId = result.Principal.FindFirst(ClaimTypes.Email).Value;

            var identity = new ClaimsIdentity(
                    authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                    nameType: Claims.Name,
                    roleType: Claims.Role
                );

            identity.SetClaim(Claims.Subject, userId)
                .SetClaim(Claims.Email, userId)
                .SetClaim(Claims.Name, userId)
                .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

            identity.SetScopes(request.GetScopes());
            identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            identity.SetDestinations(c => AuthService.GetDestinations(identity, c));

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/token")]
        public async Task<IActionResult> Exchange()
        {
            var request = HttpContext.GetOpenIddictServerRequest() ??
                throw new InvalidOperationException("The OpenID Connect request is missing.");

            if (!request.IsAuthorizationCodeGrantType())
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            var result =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal.GetClaim(Claims.Subject);

            if (string.IsNullOrEmpty(userId))
            {
                return Forbid(
                   authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                   properties: new AuthenticationProperties(new Dictionary<string, string?>
                   {
                       [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidGrant,
                       [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "Cannot find user from the token."
                   }));
            }

            var identity = new ClaimsIdentity(result.Principal.Claims,
                  authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                  nameType: Claims.Name,
                  roleType: Claims.Role);

            identity.SetClaim(Claims.Subject, userId)
                  .SetClaim(Claims.Email, userId)
                  .SetClaim(Claims.Name, userId)
                  .SetClaims(Claims.Role, new List<string> { "user", "admin" }.ToImmutableArray());

            identity.SetDestinations(c => AuthService.GetDestinations(identity, c));

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/connect/logout")]
        public async Task<IActionResult> LogoutPost()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return SignOut(
                  authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                  properties: new AuthenticationProperties
                  {
                      RedirectUri = "/"
                  });
        }
    }
}
