using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SefazIdentity;

namespace SefazLib.IdentityCfg
{
    public class IdentityConfig
    {
        private static IConfiguration configuration;
        public Action<WsFederationOptions> WSFederationOptions { get; private set; }
        public Action<CookieAuthenticationOptions> CookieAuthenticationOptions { get; private set; }
        public Action<Microsoft.AspNetCore.Authentication.AuthenticationOptions> AuthenticationOptions { get; private set; }
        public Action<OpenIdConnectOptions> OpenIdConnectOptions { get; private set; }
        public static Boolean Logoff { get; private set; }
        public IdentityConfig(IConfiguration Configuration)
        {
            Logoff = false;
            configuration = Configuration;

            AuthenticationOptions = options =>
            {
                switch (Configuration["identity:type"])
                {
                    case "jwt":
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        break;
                    case ("openid" or "azuread"):
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                        break;
                    default:
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
                        break;
                }
            };

            WSFederationOptions = options =>
            {
                options.Wtrealm = configuration["identity:realm"];
                options.MetadataAddress = configuration["identity:metadataaddress"];

                if (Configuration["identity:type"] == "sefazidentity")
                {
                    options.Wreply = configuration["identity:reply"];
                    options.Events.OnRedirectToIdentityProvider = OnRedirectToIdentityProvider;
                    options.Events.OnSecurityTokenReceived = OnSecurityTokenReceived;
                    options.TokenValidationParameters = new TokenValidationParameters { SaveSigninToken = true };
                    options.CorrelationCookie = new CookieBuilder
                    {
                        Name = ".Correlation.",
                        HttpOnly = true,
                        IsEssential = true,
                        SameSite = SameSiteMode.None,
                        SecurePolicy = CookieSecurePolicy.Always,
                        Expiration = new TimeSpan(0, 0, 15, 0),
                        MaxAge = new TimeSpan(0, 0, 15, 0)
                    };
                }
            };

            if (Configuration["identity:type"] == "openid")
            {
                OpenIdConnectOptions = options =>
                {
                    options.ClientId = configuration["identity:clientid"];
                    options.Authority = configuration["identity:authority"];
                    options.MetadataAddress = configuration["identity:metadataaddess"];
                    options.SignedOutRedirectUri = configuration["identity:realm"];
                    options.SignInScheme = "Cookies";
                    options.RequireHttpsMetadata = true;
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.UsePkce = false;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.SaveTokens = true;

                    options.Events = new OpenIdConnectEvents
                    {
                        OnRemoteFailure = OnAuthenticationFailed
                    };
                };
            };

            CookieAuthenticationOptions = options =>
            {
                options.Cookie = new CookieBuilder
                {
                    Name = "FedAuth",
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.None,
                    SecurePolicy = CookieSecurePolicy.Always
                };
                options.ExpireTimeSpan = new TimeSpan(0, 0, int.Parse(configuration["identity:timeout"]), 0);
                options.SlidingExpiration = false;
            };
        }

        public static async Task Logout(HttpContext httpContext, IConfiguration Configuration)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            switch (Configuration["identity:type"])
            {
                case "jwt":
                    await httpContext.SignOutAsync(JwtBearerDefaults.AuthenticationScheme);
                    break;
                case ("openid" or "azuread"):
                    await httpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                    break;
                default:
                    await httpContext.SignOutAsync(WsFederationDefaults.AuthenticationScheme);
                    break;
            }

            Logoff = true;
        }

        private static async Task<Task<int>> OnSecurityTokenReceived(SecurityTokenReceivedContext arg)
        {
            TokenWSClient tokenWS = new TokenWSClient(TokenWSClient.EndpointConfiguration.TokenWS, configuration["identity:tokenws"]);
            try
            {
                if (await tokenWS.IsTokenValidAsync(arg.ProtocolMessage.GetToken(), configuration["identity:realm"], "00031C33"))
                {
                    return Task.FromResult(0);
                }
            }
            finally
            {
                #region Close_or_Abort
                if (tokenWS != null)
                {
                    try
                    {
                        await tokenWS.CloseAsync();
                    }
                    catch (Exception)
                    {
                        tokenWS.Abort();
                    }
                }
                #endregion
            }
            throw new Exception($"Token recebido é inválido ou não foi emitido para '{configuration["identity:realm"]}'.");
        }

        public static Task OnRedirectToIdentityProvider(Microsoft.AspNetCore.Authentication.WsFederation.RedirectContext arg)
        {
            arg.ProtocolMessage.Wauth = configuration["identity:Wauth"];
            arg.ProtocolMessage.Wfresh = configuration["identity:timeout"];
            arg.ProtocolMessage.Parameters.Add("ClaimSets", "80000000");
            arg.ProtocolMessage.Parameters.Add("TipoLogin", "00031C33");
            arg.ProtocolMessage.Parameters.Add("AutoLogin", "0");
            arg.ProtocolMessage.Parameters.Add("Layout", "2");
            return Task.FromResult(0);
        }

        public static Task OnAuthenticationFailed(RemoteFailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/?errormessage=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}
