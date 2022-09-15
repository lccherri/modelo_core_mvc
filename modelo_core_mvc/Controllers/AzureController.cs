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
    public class AzureController : BaseController
    {
        private readonly AzureUtil azureUtil;
        private ListModel listModel { get; set; }

        public AzureController(IConfiguration Configuration, AzureUtil AzureUtil, ListModel ListModel)
        {
            azureUtil = AzureUtil;
            listModel = ListModel;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var graphClient = azureUtil.ObterGraphClient("Delegated");
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
        public ActionResult AdicionarLinha()
        {
            ViewData["Title"] = "Nova linha";
            ViewData["Message"] = "Incluir nova linha na lista";
            return View(new ListModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AdicionarLinha(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }

        [HttpGet]
        public ActionResult AlterarLinha()
        {
            ViewData["Title"] = "Editar informações";
            ViewData["Message"] = "Modificar as informações da linha";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AlterarLinha(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }

        [HttpGet]
        public ActionResult ExcluirLinha()
        {
            ViewData["Title"] = "Excluir linha";
            ViewData["Message"] = "Exclusão da linha";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ExcluirLinha(ListModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }
    }
}
