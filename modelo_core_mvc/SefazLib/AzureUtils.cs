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
using System.Diagnostics;
using System.Net.Http.Headers;
using SefazLib.usuarios;

namespace SefazLib.AzureUtils
{
    public class AzureUtil
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly string tenantId;
        private readonly string clientId;
        private readonly string clientSecret;
        public string graphToken;
        public Dictionary<string, string> jwtToken;
        private readonly ITokenAcquisition tokenAcquisition;

        //Autenticação com AzureAD
        public AzureUtil(HttpClient HttpClient, IConfiguration Configuration, ITokenAcquisition TokenAcquisition)
        {
            httpClient = HttpClient;
            configuration = Configuration;
            clientId = configuration["AzureAd:ClientId"];
            clientSecret = configuration["AzureAd:ClientSecret"];
            tenantId = configuration["AzureAd:TenantId"];
            tokenAcquisition = TokenAcquisition;
        }

        //Autenticação sem AzureAD
        public AzureUtil(HttpClient HttpClient, IConfiguration Configuration)
        {
            httpClient = HttpClient;
            configuration = Configuration;
            clientId = Configuration["AzureAd:ClientId"];
            clientSecret = Configuration["AzureAd:ClientSecret"];
            tenantId = Configuration["AzureAd:TenantId"];
        }

        //Preparação do http ara autenticacao de api
        public async Task PrepareAuthenticatedClient()
        {
            if (configuration["identity:type"] == "azuread")
            {
                string[] initialScopes = configuration.GetValue<string>("CallApi:ScopeForAccessToken")?.Split(' ').ToArray();
                string accessToken = "";
                try
                {
                    accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(initialScopes);
                }
                catch (System.Exception e)
                {
                    Debug.WriteLine(e.Message);
                    throw;
                }
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            };

            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<Usuario> GetUserAsync()
        {
            if (configuration["identity:type"] == "azuread")
            {
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
                    GraphServiceClient graphClientApplication = await ObterGraphClientApplicationAsync();
                    userAzure = await graphClientApplication.Me
                        .Request()
                        .GetAsync();
                }

                return new Usuario(userAzure.Id, userAzure.GivenName, userAzure.DisplayName, userAzure.JobTitle, userAzure.Mail, fotoUsuario); 
            }
            else
            {
                return new Usuario();
            }

        }

        public async Task<GraphServiceClient> ObterGraphClientApplicationAsync()
        {
            string[] scopes = configuration.GetValue<string>("CallApi:MicrosoftGraphDefault")?.Split(' ').ToArray();

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            ClientSecretCredential clientSecretCredential = new ClientSecretCredential(tenantId,
                                                                    clientId,
                                                                    clientSecret,
                                                                    options);

            await obterGraphToken(scopes, clientSecretCredential);

            return new GraphServiceClient(clientSecretCredential, scopes);
        }

        public async Task<GraphServiceClient> ObterGraphClientDelegatedAsync()
        {
            var scopes = new[] { "User.Read" };
            await obterGraphToken(scopes, null);

            var authProvider = new DelegateAuthenticationProvider(async (request) =>
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", graphToken);
            });

            return new GraphServiceClient(authProvider);
        }

        private async Task<string> obterGraphToken(string[] scopes, ClientSecretCredential clientSecretCredential)
        {
            if (clientSecretCredential is null)
            {
                graphToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes);
            }
            else
            {
                graphToken = clientSecretCredential!.GetTokenAsync(new TokenRequestContext(scopes)).Result.Token;
            }

            jwtToken = GetTokenInfo(graphToken);

            return graphToken;
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
