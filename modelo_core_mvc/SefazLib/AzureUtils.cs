using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using SefazLib.usuarios;
using SefazLib.IdentityCfg;

namespace SefazLib.AzureUtils
{
    public class AzureUtil
    {
        public HttpClient httpClient;
        public string erro;
        public string[] scopes;
        public Dictionary<string, string> tokenInfo;
        private readonly IConfiguration configuration;
        private readonly IdentityConfig identityConfig;

        public AzureUtil(IConfiguration Configuration, IdentityConfig IdentityConfig)
        {
            httpClient = new HttpClient();
            configuration = Configuration;
            identityConfig = IdentityConfig;
        }

        public async Task<Usuario> GetUserAsync()
        {
            if (configuration["identity:type"] == "azuread")
            {
                string fotoUsuario = null;
                Microsoft.Graph.User userAzure;
                try
                {
                    GraphServiceClient graphClientDelegated = ObterGraphClient("");
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
                    return new Usuario(userAzure.Id, userAzure.GivenName, userAzure.DisplayName, userAzure.JobTitle, userAzure.Mail, fotoUsuario);
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

        public GraphServiceClient ObterGraphClient(string tipoClient)
        {
            switch (tipoClient)
            {
                case "Application":
                    return new GraphServiceClient(new ClientSecretCredential(configuration["AzureAd:ClientId"], configuration["AzureAd:TenantId"], configuration["AzureAd:ClientSecret"]));

                default:
                    identityConfig.SetScope("MSGraph");
                    var authProvider = new DelegateAuthenticationProvider(async (request) =>
                    {
                        request.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await identityConfig.obterAccessToken(null));
                    });
                    return new GraphServiceClient(authProvider);
            }
        }

        public async Task<string> buscaSiteId(string siteNome)
        {
            var graphClient = ObterGraphClient("Delegated");
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
            var graphClient = ObterGraphClient("Delegated");
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
