using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http;

namespace modelo_core_mvc.Controllers
{
    //[AuthorizeForScopes(Scopes = new[] { "user.read" })]
    public class AzureController : Controller
    {
        private readonly IConfiguration Configuration;

        public AzureController(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        [Authorize]
        public async Task<ViewResult> DadosUser()
        {
            var tenantId     = Configuration["AzureAd:TenantId"];
            var clientId     = Configuration["AzureAd:ClientId"];
            var clientSecret = Configuration["AzureAd:ClientSecret"];
            var redirect_uri = Configuration["Identity:realm"];
            var graphRoot    = Configuration.GetValue<string>("CallApi:MicrosoftGraph")?.Split(' ').FirstOrDefault();
            var scopes = new[] { "User.Read" };
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var url = $"https://login.microsoftonline.com/{tenantId}"+
                      $"/oauth2/v2.0/authorize?client_id={clientId}" +
                      $"&response_type=code" +
                      $"&redirect_uri={redirect_uri}" +
                      $"&response_mode=query&scope={graphRoot}{scopes[0]}";

            try
            {
                var authorizationCode = "";

                var httpClient = new HttpClient();
                //httpClient.BaseAddress = new System.Uri(Configuration["apiendereco:projetos"]);
                //httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                var resposta = httpClient.GetAsync(url).Result;
                var conteudo = resposta.Content.ReadAsStringAsync();
                
                var authCodeCredential = new AuthorizationCodeCredential(
                    tenantId, clientId, clientSecret, authorizationCode, options);

                var graphClient = new GraphServiceClient(authCodeCredential, scopes);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                throw;
            }

            //var graphClient = ObterGraphClientAsync("Delegated");
            //ViewData["token"] = graphToken;

            try
            {
                //var user = graphClient.Request().GetAsync();
                //ViewData["login"] = user;
                //ViewData["nome"] = user.DisplayName;

                //using (var photoStream = await _graphServiceClient.Me.Photo.Content.Request().GetAsync())
                //{
                //    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                //    ViewData["foto"] = Convert.ToBase64String(photoByte);
                //}
            }
            catch (Exception ex)
            {
                ViewData["foto"] = null;
            }
            return View();
        }
        
        public async Task<IActionResult> MicrosoftGraphAsync()
        {
            var graphClient = await ObterGraphClientAsync("Application");
            //ViewData["token"] = graphToken;

            var siteId = "fazendaspgovbr.sharepoint.com,6d117106-a0df-4b73-8834-99756806b907,37489eab-f4d0-4ad0-8031-886758dade5f";
            var listaId = "2306a558-a803-4e25-955e-3136deed7c00";

            var sites = graphClient.Sites;
            var site = sites[siteId];

            try
            {
                var colunas = await graphClient.Sites[siteId].Lists[listaId].Columns
                    .Request()
                    .GetAsync();

                string[] nomesColunas = new string[colunas.Count];
                foreach (ColumnDefinition column in colunas)
                {
                    nomesColunas[colunas.IndexOf(column)] = column.Name;
                }

                var drives = await graphClient.Sites[siteId].Drives
                    .Request()
                    .GetAsync();

                var queryOptions = new List<QueryOption>() { new QueryOption("expand", "fields(select=Item,Title,Attachemnts,teste)") };
                var items = await graphClient.Sites[siteId].Lists[listaId].Items.Request(queryOptions).GetAsync();

                ViewData["mensagem"] = drives.ToString();
            }
            catch (System.Exception e)
            {
                ViewData["mensagem"] = e.Message;
            }

            return View();
        }
        
        private async Task<GraphServiceClient> ObterGraphClientAsync(string tipo)
        {
            var tenantId = Configuration["AzureAd:TenantId"];
            var clientId = Configuration["AzureAd:ClientId"];
            var clientSecret = Configuration["AzureAd:ClientSecret"];

            //siteId -> https://fazendaspgovbr-my.sharepoint.com/personal/login_fazenda_sp_gov_br/_api/site/id

            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            string[] scopes = Configuration.GetValue<string>("CallApi:MicrosoftGraphDefault")?.Split(' ').ToArray();
            var clientSecretCredential = new ClientSecretCredential(tenantId,
                                                                    clientId,
                                                                    clientSecret,
                                                                    options);

            var tokenRequestContext = new TokenRequestContext(scopes);
            //graphToken = clientSecretCredential!.GetTokenAsync(tokenRequestContext).Result.Token;

            return new GraphServiceClient(clientSecretCredential, scopes);
        }

    }
}
