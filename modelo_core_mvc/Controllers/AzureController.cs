using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using SefazLib.AzureUtils;
using modelo_core_mvc.Models;

namespace modelo_core_mvc.Controllers
{
    //[AuthorizeForScopes(Scopes = new[] { "user.read" })]
    public class AzureController : Controller
    {
        private readonly AzureUtil azureUtil;
        private ListModel listModel { get; set; }

        public AzureController(IConfiguration Configuration, AzureUtil AzureUtil, ListModel ListModel)
        {
            azureUtil = AzureUtil;
            listModel = ListModel;
        }


        [Authorize]
        //Chamada do MS Graph com permission do tipo Delegated
        //Nesse tipo, é utilizada a autenticação do usuário para concessão de permissão
        public async Task<ViewResult> MSGraphDelegatedAsync()
        {
            //"https://graph.microsoft.com/v1.0/sites/fazendaspgovbr.sharepoint.com/:/sites/PreparaConformes"

            GraphServiceClient graphClientDelegated = await azureUtil.ObterGraphClientDelegatedAsync();

            ViewData["login"] = azureUtil.jwtToken["upn"];
            ViewData["nome"] = azureUtil.jwtToken["name"];
            ViewData["scp"] = azureUtil.jwtToken["scp"];
            ViewData["token"] = azureUtil.graphToken;

            return View();
        }

        //Chamada do MS Graph com permission do tipo Application
        //Nesse tipo, é utilizada a autenticação da aplicação, com uso de Secret ou Certificate, para concessão de permissão
        public async Task<IActionResult> List()
        {
            GraphServiceClient graphClient = await azureUtil.ObterGraphClientApplicationAsync();
            ViewData["app_name"] = azureUtil.jwtToken["app_displayname"];
            ViewData["roles"]    = azureUtil.jwtToken["roles"];
            ViewData["token"]    = azureUtil.graphToken;

            var lista = new List<ListModel>();

            var siteId = "fazendaspgovbr.sharepoint.com,6d117106-a0df-4b73-8834-99756806b907,37489eab-f4d0-4ad0-8031-886758dade5f";
            ViewData["siteId"] = siteId;

            var listaId = "2306a558-a803-4e25-955e-3136deed7c00";

            var sites = graphClient.Sites;
            var site = sites[siteId];

            try
            {
                var colunas = await site.Lists[listaId].Columns
                    .Request()
                    .GetAsync();

                string[] nomesColunas = new string[colunas.Count];
                foreach (ColumnDefinition column in colunas)
                {
                    nomesColunas[colunas.IndexOf(column)] = column.Name;
                }

                var drives = await site.Drives
                    .Request()
                    .GetAsync();

                var queryOptions = new List<QueryOption>() { new QueryOption("expand", "fields(select=Item,Title,Attachemnts,teste)") };
                var items = await site.Lists[listaId].Items.Request(queryOptions).GetAsync();
                foreach (var item in items)
                {
                    var valores = new List<string>();
                    foreach (var dado in item.Fields.AdditionalData)
                    {
                        valores.Add(dado.Value.ToString());
                    }
                    lista.Add(new ListModel(valores[1], valores[2]));
                }

                ViewData["mensagem"] = string.Format("Itens da lista {0}", site.Lists[listaId]);
            }
            catch (System.Exception e)
            {
                ViewData["mensagem"] = e.Message;
            }

            return View(lista);
        }

        [HttpGet]
        public ActionResult Adicionar()
        {
            ViewData["Title"] = "Novo Projeto";
            ViewData["Message"] = "Incluir novo projeto";
            return View(new ListModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Adicionar(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }

        [HttpGet]
        public ActionResult Alterar()
        {
            ViewData["Title"] = "Editar Projeto";
            ViewData["Message"] = "Editar informações do projeto";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Alterar(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }

        [HttpGet]
        public ActionResult Excluir()
        {
            ViewData["Title"] = "Excluir Projeto";
            ViewData["Message"] = "Exclusão do projeto";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Excluir(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }
    }
}
