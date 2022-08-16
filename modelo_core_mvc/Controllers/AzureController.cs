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
            IGraphServiceSitesCollectionPage items;
            GraphServiceClient graphClient;
            try
            {
                graphClient = await azureUtil.ObterGraphClientHttpAsync();
                items = await graphClient.Sites
                        .Request()
                        .GetAsync();
            }
            catch (System.Exception)
            {
                graphClient = await azureUtil.ObterGraphClientDelegatedAsync();
                items = await graphClient.Sites
                        .Request()
                        .GetAsync();
            }

            var displayName = new List<string>();
            foreach (var item in items)
            {
                displayName.Add(item.DisplayName);
            }

            return View();
        }

        //Chamada do MS Graph com permission do tipo Application
        //Nesse tipo, é utilizada a autenticação da aplicação, com uso de Secret ou Certificate, para concessão de permissão
        public async Task<IActionResult> List()
        {
            GraphServiceClient graphClient = azureUtil.ObterGraphClientApplication();

            var items = await graphClient.Sites
                    .Request()
                    .GetAsync();

            var displayName = new List<string>();
            foreach (var item in items)
            {
                displayName.Add(item.DisplayName);
            }

            var lista = new List<ListModel>();
            try
            {
                //var listaGraph = root.Lists[0];
                //SiteWithPath("PreparaConformes").
                //GetByPath("/Lists/FotosDiligencias");

                //var colunas = await site.Lists[listaId].Columns
                //var colunas = listaGraph.
                //              Columns
                //              .Request()
                //              .GetAsync();

                //string[] nomesColunas = new string[colunas.Count];
                //foreach (ColumnDefinition column in colunas)
                //{
                //    nomesColunas[colunas.IndexOf(column)] = column.Name;
                //}

                //var drives = await site.Drives
                //    .Request()
                //    .GetAsync();

                //var queryOptions = new List<QueryOption>() { new QueryOption("expand", "fields(select=Item,Title,Attachemnts,teste)") };
                //var items = await listaGraph.Items.Request(queryOptions).GetAsync();
                //foreach (var item in items)
                //{
                //    var valores = new List<string>();
                //    foreach (var dado in item.Fields.AdditionalData)
                //    {
                //        valores.Add(dado.Value.ToString());
                //    }
                //    listaGraph.Add(new ListModel(valores[1], valores[2]));
                //}

                //ViewData["mensagem"] = string.Format("Itens da lista {0}", site.Lists[listaId]);
            }
            catch (System.Exception e)
            {
                ViewData["mensagem"] = e.Message;
            }

            return View(lista);
        }

        //Chamada do MS Graph com permission do tipo Application
        //Nesse tipo, é utilizada a autenticação da aplicação, com uso de Secret ou Certificate, para concessão de permissão
        public async Task<IActionResult> MSGraphApplicationAsync()
        {
            GraphServiceClient graphClient = azureUtil.ObterGraphClientApplication();

            var items = await graphClient.Sites
                    .Request()
                    .GetAsync();

            var displayName = new List<ListModel>();
            foreach (var item in items)
            {
                displayName.Add(new ListModel(item.DisplayName, item.Id));
            }

            return View(displayName);
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
