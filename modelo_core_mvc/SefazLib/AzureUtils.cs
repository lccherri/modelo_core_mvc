using Azure.Core;
using Azure.Identity;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using SefazLib.usuarios;

namespace SefazLib.AzureUtils
{
    public class AzureUtil
    {
        public HttpClient httpClient;
        public Dictionary<string, string> jwtToken;
        private readonly IConfiguration configuration;
        private readonly string tenantId;
        private readonly string clientId;
        private readonly string clientSecret;
        private readonly ITokenAcquisition tokenAcquisition;

        //Autenticação com AzureAD
        public AzureUtil(IConfiguration Configuration, ITokenAcquisition TokenAcquisition)
        {
            httpClient = new HttpClient();
            configuration = Configuration;
            clientId = configuration["AzureAd:ClientId"];
            clientSecret = configuration["AzureAd:ClientSecret"];
            tenantId = configuration["AzureAd:TenantId"];
            tokenAcquisition = TokenAcquisition;
        }

        //Autenticação sem AzureAD
        public AzureUtil(IConfiguration Configuration)
        {
            httpClient = new HttpClient();
            configuration = Configuration;
            clientId = Configuration["AzureAd:ClientId"];
            clientSecret = Configuration["AzureAd:ClientSecret"];
            tenantId = Configuration["AzureAd:TenantId"];
        }

        //Preparação do http ara autenticacao de api
        public async Task<AuthenticationHeaderValue> AuthenticationHeader(string[] scopes)
        {
            if (configuration["identity:type"] == "azuread")
            {
                if (!scopes.Any()) { scopes = configuration.GetValue<string>("CallApi:ScopeForAccessToken")?.Split(' ').ToArray(); }
                var accessToken = await obterAccessToken(scopes, null);
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
            return httpClient.DefaultRequestHeaders.Authorization;
        }

        public async Task<Usuario> GetUserAsync()
        {
            if (configuration["identity:type"] == "azuread")
            {
                string[] scopes = { "User.Read" };
                var accessToken = await obterAccessToken(scopes, null);
                string fotoUsuario = null;
                Microsoft.Graph.User userAzure;
                try
                {
                    GraphServiceClient graphClientDelegated = await ObterGraphClientDelegatedAsync();
                    userAzure = await graphClientDelegated.Me
                        .Request()
                        .GetAsync();

                    try
                    {
                        // Get user photo
                        using (var photoStream = await graphClientDelegated.Me.Photo.Content.Request().GetAsync())
                        {
                            byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                            fotoUsuario = Convert.ToBase64String(photoByte);
                        }
                    }
                    catch (System.Exception)
                    {
                        fotoUsuario = null;
                    }
                }
                catch (System.Exception)
                {
                    GraphServiceClient graphClientApplication = ObterGraphClientApplication();
                    userAzure = await graphClientApplication.Me
                        .Request()
                        .GetAsync();
                }

                return new Usuario(userAzure.Id, userAzure.GivenName, userAzure.DisplayName, userAzure.JobTitle, userAzure.Mail, fotoUsuario, accessToken);
            }
            else
            {
                return new Usuario();
            }

        }

        public GraphServiceClient ObterGraphClientApplication()
        {
            string[] scopes = { "https://graph.microsoft.com/.default" };
            var options = new TokenCredentialOptions { AuthorityHost = AzureAuthorityHosts.AzurePublicCloud };
            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(tenantId,
                                                                    clientId,
                                                                    clientSecret,
                                                                    options);
            return new GraphServiceClient(clientSecretCredential, scopes);
        }

        public async Task<GraphServiceClient> ObterGraphClientDelegatedAsync()
        {
            var scopes = new[] { "User.Read" };
            ;
            var authProvider = new DelegateAuthenticationProvider(async (request) =>
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await obterAccessToken(scopes, null));
            });
            return new GraphServiceClient(authProvider);
        }

        public async Task<GraphServiceClient> ObterGraphClientHttpAsync()
        {
            var scopes = new[] { "User.Read" };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await obterAccessToken(scopes, null));
            return new GraphServiceClient(httpClient);
        }

        private async Task<string> obterAccessToken(string[] scopes, ClientSecretCredential clientSecretCredential)
        {
            string accessToken;
            if (clientSecretCredential is null)
            {
                accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            }
            else
            {
                accessToken = clientSecretCredential!.GetTokenAsync(new TokenRequestContext(scopes)).Result.Token;
            }

            jwtToken = GetTokenInfo(accessToken);

            return accessToken;
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
    }
}
