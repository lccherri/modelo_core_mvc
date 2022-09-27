using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using SefazIdentity;
using System.Net.Http;
using System.Collections.Generic;
using Microsoft.Identity.Web;
using System.Net.Http.Headers;
using Azure.Identity;
using Azure.Core;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;

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
        public HttpClient httpClient;
        public string jwtToken;
        public string erro;
        public string[] scopes;
        public Dictionary<string, string> tokenInfo;
        private readonly ITokenAcquisition tokenAcquisition;

        public IdentityConfig(IConfiguration Configuration)
        {
            httpClient = new HttpClient();
            configuration = Configuration;
            Logoff = false;

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

        public async Task<AuthenticationHeaderValue> AuthenticationHeader()
        {
            if (configuration["identity:type"] == "azuread")
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await obterAccessToken(null));
            }
            return httpClient.DefaultRequestHeaders.Authorization;
        }

        #region Azure AD
        public IdentityConfig(IConfiguration Configuration, ITokenAcquisition TokenAcquisition)
        {
            tokenAcquisition = TokenAcquisition;
            httpClient = new HttpClient();
            configuration = Configuration;
            Logoff = false;

            AuthenticationOptions = options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            };

            WSFederationOptions = options =>
            {
                options.Wtrealm = configuration["identity:realm"];
                options.MetadataAddress = configuration["identity:metadataaddress"];
            };

        }

        public void SetScope(string callApi)
        {
            scopes = configuration.GetValue<string>("CallApi:" + callApi)?.Split(' ').ToArray();
        }

        public async Task<string> obterAccessToken(ClientSecretCredential clientSecretCredential)
        {
            try
            {
                if (clientSecretCredential is null)
                {
                    jwtToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
                }
                else
                {
                    jwtToken = clientSecretCredential!.GetTokenAsync(new TokenRequestContext(scopes)).Result.Token;
                }
                tokenInfo = GetTokenInfo(jwtToken);
            }
            catch (Exception ex)
            {
                erro = ex.Message;
            }
            return jwtToken;
        }
        protected Dictionary<string, string> GetTokenInfo(string token)
        {
            var TokenInfo = new Dictionary<string, string>();

            var handler = new JwtSecurityTokenHandler();
            var jwtSecurityToken = handler.ReadJwtToken(token);
            var claims = jwtSecurityToken.Claims.ToList();

            foreach (var claim in claims)
            {
                if (!TokenInfo.ContainsKey(claim.Type))
                {
                    TokenInfo.Add(claim.Type, claim.Value);
                }
            }

            return TokenInfo;
        }
        #endregion

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

        #region
          
        #endregion
    }
}
