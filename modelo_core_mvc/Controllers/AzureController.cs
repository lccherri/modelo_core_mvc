using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using SefazLib.AzureUtils;
using modelo_core_mvc.Models;
using System.Linq;

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

        public async Task<IActionResult> MSGraphApplicationAsync()
        {
            var graphClient = await azureUtil.ObterGraphClientAsync("Application");
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

        [Authorize]
        public async Task<IActionResult> List()
        {
            var graphClient = await azureUtil.ObterGraphClientAsync("Delegated");

            string siteId = await azureUtil.buscaSiteId("CA-DTI-CAP");
            string listaId = await azureUtil.buscaListaId("Modelo_core", siteId);

            var listaGraph = await graphClient
                    .Sites[siteId]
                    .Lists[listaId]
                    .Columns
                    .Request()
                    .GetAsync();

            var lista = new List<ListModel>();
            if (listaGraph is not null)
            {
                var colunas = listaGraph;

                string[] nomesColunas = new string[colunas.Count];
                foreach (ColumnDefinition column in colunas)
                {
                    nomesColunas[colunas.IndexOf(column)] = column.Name;
                }

                var queryOptions = new List<QueryOption>() { new QueryOption("expand", "fields(select=Title,Coluna1,Numero)") };
                var linhas = await graphClient
                    .Sites[siteId]
                    .Lists[listaId]
                    .Items
                    .Request(queryOptions)
                    .GetAsync();

                foreach (var linha in linhas)
                {
                    var valores = new List<string>();
                    foreach (var dado in linha.Fields.AdditionalData)
                    {
                        valores.Add(dado.Value.ToString());
                    }
                    lista.Add(new ListModel(valores[2], valores[1], double.Parse(valores[3])));
                }
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
