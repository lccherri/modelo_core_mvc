using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using SefazLib.MSGraphUtils;
using modelo_core_mvc.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Text;

namespace modelo_core_mvc.Controllers
{
    //[AuthorizeForScopes(Scopes = new[] { "user.read" })]
    public class AzureController : Controller
    {
        private readonly MSGraphUtil mSGraphUtil;
        private MSListTesteModel mSListTesteModel { get; set; }

        public AzureController(IConfiguration Configuration, MSGraphUtil MSGraphUtil, MSListTesteModel MSListTesteModel)
        {
            mSGraphUtil = MSGraphUtil;
            mSListTesteModel = MSListTesteModel;
        }


        [Authorize]
        //Chamada do MS Graph com permission do tipo Delegated
        //Nesse tipo, é utilizada a autenticação do usuário para concessão de permissão
        public async Task<ViewResult> MSGraphDelegatedAsync()
        {
            //"https://graph.microsoft.com/v1.0/sites/fazendaspgovbr.sharepoint.com/:/sites/PreparaConformes"

            GraphServiceClient graphClientDelegated = await mSGraphUtil.ObterGraphClientDelegatedAsync();

            ViewData["login"] = mSGraphUtil.jwtToken["upn"];
            ViewData["nome"] = mSGraphUtil.jwtToken["name"];
            ViewData["scp"] = mSGraphUtil.jwtToken["scp"];
            ViewData["token"] = mSGraphUtil.graphToken;

            return View();
        }

        //Chamada do MS Graph com permission do tipo Application
        //Nesse tipo, é utilizada a autenticação da aplicação, com uso de Secret ou Certificate, para concessão de permissão
        public async Task<IActionResult> MSListAsync()
        {
            GraphServiceClient graphClient = await mSGraphUtil.ObterGraphClientApplicationAsync();
            ViewData["app_name"] = mSGraphUtil.jwtToken["app_displayname"];
            ViewData["roles"]    = mSGraphUtil.jwtToken["roles"];
            ViewData["token"]    = mSGraphUtil.graphToken;

            var lista = new List<MSListTesteModel>();

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
                    lista.Add(new MSListTesteModel(valores[1], valores[2]));
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
            return View(new MSListTesteModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Adicionar(MSListTesteModel model)
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
        public ActionResult Alterar(MSListTesteModel model)
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
        public ActionResult Excluir(MSListTesteModel model)
        {
            if (ModelState.IsValid)
            {
                return RedirectToAction("Index");
            }
            return BadRequest();
        }
    }
}
