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
        public string jwtToken;
        public string erro;
        public string[] scopes;
        public Dictionary<string, string> tokenInfo;
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

        private async Task<string> obterAccessToken(ClientSecretCredential clientSecretCredential)
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

        //Preparação do http ara autenticacao de api
        public async Task<AuthenticationHeaderValue> AuthenticationHeader()
        {
            if (configuration["identity:type"] == "azuread")
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await obterAccessToken(null));
            }
            return httpClient.DefaultRequestHeaders.Authorization;
        }

        public void SetScope(string callApi)
        {
            scopes = configuration.GetValue<string>("CallApi:" + callApi)?.Split(' ').ToArray();
        }
        public async Task<Usuario> GetUserAsync()
        {
            if (configuration["identity:type"] == "azuread")
            {
                string fotoUsuario = null;
                Microsoft.Graph.User userAzure;
                try
                {
                    GraphServiceClient graphClientDelegated = await ObterGraphClientAsync("");
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
                    catch (System.Exception ex)
                    {
                        fotoUsuario = null;
                        erro = ex.Message;
                    }
                    return new Usuario(userAzure.Id, userAzure.GivenName, userAzure.DisplayName, userAzure.JobTitle, userAzure.Mail, fotoUsuario, jwtToken);
                }
                catch (System.Exception ex)
                {
                    erro = ex.Message;
                    return new Usuario();
                }

            }
            else
            {
                return new Usuario();
            }

        }

        public async Task<GraphServiceClient> ObterGraphClientAsync(string tipoClient)
        {
            switch (tipoClient)
            {
                case "Application":
                    return new GraphServiceClient(new ClientSecretCredential(tenantId, clientId, clientSecret));

                case "Http":
                    SetScope("MSGraph");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await obterAccessToken(null));
                    return new GraphServiceClient(httpClient, "https://graph.microsoft.com/v1.0");

                default:
                    SetScope("MSGraph");
                    var authProvider = new DelegateAuthenticationProvider(async (request) =>
                    {
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await obterAccessToken(null));
                    });
                    return new GraphServiceClient(authProvider);
            }
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

        public async Task<string> buscaSiteId(string siteNome)
        {
            var graphClient = await ObterGraphClientAsync("Delegated");
            var requests = new List<SearchRequestObject>()
            {
                new SearchRequestObject
                {
                    EntityTypes = new List<EntityType>()
                    {
                        EntityType.Site
                    },
                    Query = new SearchQuery
                    {
                        QueryString = siteNome
                    },
                    From = 0,
                    Size = 25
                }
            };

            var result = await graphClient.Search
                    .Query(requests)
                    .Request()
                    .PostAsync();

            string siteId = "";
            if ((result.CurrentPage.Count > 0) && (result.CurrentPage[0].HitsContainers.Count() > 0))
            {
                var hitContainer = result.CurrentPage[0].HitsContainers.First();
                if (hitContainer.Hits != null)
                {
                    foreach (var hit in hitContainer.Hits)
                    {
                        var recurso = await graphClient.Sites[hit.HitId].Request().GetAsync();
                        siteId = hit.HitId;
                        if (recurso.DisplayName == siteNome) { break; }
                    }
                }
            }

            return siteId;
        }

        public async Task<string> buscaListaId(string listaNome, string siteId)
        {
            var graphClient = await ObterGraphClientAsync("Delegated");
            var items = await graphClient.Sites[siteId].Lists
                                            .Request()
                                            .GetAsync();
            string listaId = "";
            foreach (var item in items)
            {
                if (item.DisplayName == listaNome) { listaId = item.Id; break; }
            }

            return listaId;
        }
    }
}
