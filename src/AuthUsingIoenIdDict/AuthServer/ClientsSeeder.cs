using AuthServer.Persistence;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace AuthServer
{
    public class ClientsSeeder
    {
        private readonly IServiceProvider _serviceProvider;

        public ClientsSeeder(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(_serviceProvider));
        }

        public async Task AddScopesAsync()
        {

            await using var scope = _serviceProvider.CreateAsyncScope();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            var apiScope = await manager.FindByNameAsync("api1");

            if (apiScope != null)
            { 
                await manager.DeleteAsync(apiScope);
            }

            await manager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = "api1",
                DisplayName = "Api1 scope",
                Resources = 
                {
                    "resource_server_1"
                }
            });
        }

        public async Task AddClientAsync()
        {
            await using var scopes = _serviceProvider.CreateAsyncScope();
            var dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();

            await dbContext.Database.EnsureCreatedAsync();

            var manager = _serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var client = await manager.FindByClientIdAsync("web-client");

            if (client != null)
            {
                await manager.DeleteAsync(client);
            }

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "web-client",
                ClientSecret = "901564A5-E7FE-42CB-B10D-61EF6A8F3654",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "Postman Client Application",
                RedirectUris =
                 {
                    new Uri("https://localhost:7172/swagger/oauth2-redirect.html")
                 },
                PostLogoutRedirectUris =
                 {
                    new Uri("https://localhost:7172/resources")
                 },
                Permissions = 
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.EndSession,
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Scopes.Roles,
                    $"{Permissions.Prefixes.Scope}api1"
                }
                //Requirements =
                //{
                //    Requirements.Features.ProofKeyForCodeExchange
                //}
            });
        }
    }
}
