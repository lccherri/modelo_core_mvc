using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace modelo_core_mvc.Controllers
{
    public class AzureController : Controller
    {
        private readonly IConfiguration Configuration;

        public AzureController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public async Task<IActionResult> MicrosoftGraphAsync()
        {
            string[] scopes = Configuration.GetValue<string>("CallApi:MicrosoftGraph")?.Split(' ').ToArray();

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };
            var clientSecretCredential = new ClientSecretCredential(Configuration["AzureAd:TenantId"],
                                                                    Configuration["AzureAd:ClientId"],
                                                                    Configuration["AzureAd:ClientSecret"], options);

            var tokenRequestContext = new TokenRequestContext(scopes);
            var apiToken = clientSecretCredential!.GetTokenAsync(tokenRequestContext).Result.Token;
            ViewData["token"] = apiToken;
            var siteId = "fazendaspgovbr.sharepoint.com,6d117106-a0df-4b73-8834-99756806b907,37489eab-f4d0-4ad0-8031-886758dade5f";
            var listaId = "2306a558-a803-4e25-955e-3136deed7c00";

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);
            var sites = graphClient.Sites;
            var site = sites[siteId];

            try
            {
                var lista = await graphClient.Sites[siteId].Lists[listaId]
                    .Request()
                    .GetAsync();

                var queryOptions = new List<QueryOption>() { new QueryOption("expand", "fields(select=Item,Title,Attachemnts,teste)") }; 
                var items = await graphClient.Sites[siteId].Lists[listaId].Items.Request(queryOptions).GetAsync();

                ViewData["mensagem"] = lista.ToString();
            }
            catch (System.Exception e)
            {
                ViewData["mensagem"] = e.Message;
            }

            return View();
        }

    }
}
