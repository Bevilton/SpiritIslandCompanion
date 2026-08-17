using System.IdentityModel.Tokens.Jwt;
using Application.Abstractions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace WebApp.Auth;

public static class AuthExtensions
{
    /// <summary>
    /// Configures OIDC authentication with cookie-based session management.
    /// Uses standard OIDC configuration so the identity provider can be swapped
    /// by changing appsettings (Auth0, Entra ID, Keycloak, etc.).
    /// </summary>
    public static IServiceCollection AddOidcAuthentication(this IServiceCollection services, IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var oidcSection = configuration.GetSection("Oidc");

        // Preserve original claim types from the IdP
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services.AddScoped<UserSyncOidcEvents>();

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "SICompanion.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = environment.IsDevelopment()
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);
                options.SlidingExpiration = true;
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = oidcSection["Authority"];
                options.ClientId = oidcSection["ClientId"];
                options.ClientSecret = oidcSection["ClientSecret"];

                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;

                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");

                options.SaveTokens = true;
                options.GetClaimsFromUserInfoEndpoint = true;

                // Allow HTTP metadata endpoint in development (Aspire uses HTTP by default)
                options.RequireHttpsMetadata = !environment.IsDevelopment();

                options.CallbackPath = new PathString("/callback");
                options.SignedOutCallbackPath = new PathString("/signout-callback");

                // Map OIDC claims to well-known .NET claim types
                options.TokenValidationParameters.NameClaimType = "name";

                // UserSyncOidcEvents (resolved from DI) handles both user sync on sign-in
                // and the Auth0-specific /v2/logout redirect on sign-out.
                options.EventsType = typeof(UserSyncOidcEvents);
            });

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
