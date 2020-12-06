using AspNet.Security.OpenIdConnect.Extensions;
using AspNet.Security.OpenIdConnect.Primitives;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoysCoreAPITemplate
{
    public class OpenIDServerConfiguration
    {
        public IConfigurationRoot Configuration { get; }

        public OpenIDServerConfiguration()
        {
            var dom = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", false).AddEnvironmentVariables()
                .Build();

            Configuration = dom;
        }

        public void ConfigureOpenIDServer(IServiceCollection services)
        {
            try
            {
                services.AddAuthentication("Bearer").AddOAuthValidation().AddOpenIdConnectServer(options =>
                {
#if DEBUG
                    options.AllowInsecureHttp = true;
                    options.ApplicationCanDisplayErrors = true;
#else
                    options.AllowInsecureHttp = false;
                    options.ApplicationCanDisplayErrors = false;
#endif
                    // Enable endpoints
                    options.TokenEndpointPath = "/token";
                    options.UserinfoEndpointPath = null;
                    options.AuthorizationEndpointPath = null;

                    options.UseSlidingExpiration = true; // allow refresh

                    // Implement OnValidateTokenRequest to support flows using the token endpoint
                    options.Provider.OnValidateTokenRequest = context =>
                    {
                        var scope = context.Request.Scope;

                        // Reject token requests that don't use client credentials grant or refresh grants
                        if (!context.Request.IsClientCredentialsGrantType() && !context.Request.IsRefreshTokenGrantType())
                        {
                            context.Reject(OpenIdConnectConstants.Errors.UnsupportedGrantType,
                                    "Invalid grant type specified");

                            return Task.CompletedTask;
                        }

                        if (context.Request.IsClientCredentialsGrantType() && string.IsNullOrEmpty(scope))
                        {
                            context.Reject(OpenIdConnectConstants.Errors.AccessDenied, "Please enter scope");

                            return Task.CompletedTask;
                        }

                        if (!string.Equals(context.ClientId, "Hello", StringComparison.OrdinalIgnoreCase) 
                            && string.Equals(context.ClientSecret, "World", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Reject(
                                error: OpenIdConnectConstants.Errors.InvalidRequest,
                                description: "Invalid user credentials provided");

                            return Task.CompletedTask;
                        }

                        options.AccessTokenLifetime =
                                    TimeSpan.FromDays(string.Equals(scope, "Admin", StringComparison.OrdinalIgnoreCase)
                                        ? 5 : 1);

                        options.RefreshTokenLifetime =
                            TimeSpan.FromDays(string.Equals(scope, "Admin", StringComparison.OrdinalIgnoreCase)
                                ? 90 : 7);

                        context.Validate();

                        return Task.CompletedTask;
                    };

                    // Implement OnHandleTokenRequest to support token requests
                    options.Provider.OnHandleTokenRequest = context =>
                    {
                        if (!context.Request.IsClientCredentialsGrantType())
                            return Task.CompletedTask;

                        var identity = new ClaimsIdentity(context.Scheme.Name, OpenIdConnectConstants.Claims.Name, OpenIdConnectConstants.Claims.Role);

                        // Add the mandatory subject/user identifier claim
                        identity.AddClaim(OpenIdConnectConstants.Claims.Subject,
                            context.Request.Scope,
                            OpenIdConnectConstants.Destinations.AccessToken,
                            OpenIdConnectConstants.Destinations.IdentityToken);

                        // Token role
                        identity.AddClaim(OpenIdConnectConstants.Claims.Role,
                            "HelloWorld",
                            OpenIdConnectConstants.Destinations.AccessToken,
                            OpenIdConnectConstants.Destinations.IdentityToken);

                        var ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(identity),
                            new AuthenticationProperties(),
                            context.Scheme.Name);

                        ticket.SetScopes(new List<string>
                            {
                                OpenIdConnectConstants.Scopes.OfflineAccess,
                                context.Request.Scope
                            });

                        context.Validate(ticket);

                        return Task.CompletedTask;
                    };
                });
            }
            catch (Exception e)
            {
                throw new Exception("Unable to initialize Open ID Server with error; " + e.Message, e);
            }
        }
    }
}
