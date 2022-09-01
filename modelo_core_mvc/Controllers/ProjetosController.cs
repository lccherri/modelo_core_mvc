using modelo_core_mvc.projetos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using modelo_core_mvc.ProjetosApi;

namespace modelo_core_mvc.Controllers
{
    [Authorize]
    public class ProjetosController : BaseController
    {
        private readonly ProjetosApiClient api;

        public ProjetosController(ProjetosApiClient Api)
        {
            api = Api;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ViewData["Title"] = "Projetos";
            ViewData["Message"] = "Projetos do DTI";

            return View(await api.GetProjetosAsync());
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Detalhes(long cd_projeto)
        {
            ViewData["Title"] = "Projeto";
            ViewData["Message"] = "";
            return View(await api.GetProjetoAsync(cd_projeto));
        }

        [HttpGet]
        public ActionResult Adicionar()
        {
            ViewData["Title"] = "Novo Projeto";
            ViewData["Message"] = "Incluir novo projeto";
            return View(new ProjetosModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Adicionar(ProjetosModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await api.PostProjetoAsync(model);
                    return RedirectToAction("Index");
                }
                catch 
                {
                    ViewData["Title"] = "Novo Projeto";
                    ViewData["Message"] = "Incluir novo projeto";
                    ViewData["Erro"] = "Essa aplicação não está configurada para acessar a API.";
                    return View(model);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult> Alterar(long cd_projeto)
        {
            ViewData["Title"] = "Editar Projeto";
            ViewData["Message"] = "Editar informações do projeto";
            var model = await api.GetProjetoAsync(cd_projeto);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Alterar(ProjetosModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await api.PutProjetoAsync(model);
                    return RedirectToAction("Index");
                }
                catch 
                {
                    ViewData["Erro"] = "Essa aplicação não está configurada para acessar a API.";
                    return View(model);
                }
            }
            return BadRequest();
        }

        [HttpGet]
        public async Task<ActionResult> Excluir(long cd_projeto)
        {
            ViewData["Title"] = "Excluir Projeto";
            ViewData["Message"] = "Exclusão do projeto";
            var model = await api.GetProjetoAsync(cd_projeto);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Excluir(ProjetosModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await api.DeleteProjetoAsync(model.cd_projeto);
                    return RedirectToAction("Index");
                }
                return BadRequest();
            }
            catch 
            {
                ViewData["Title"] = "Excluir Projeto";
                ViewData["Message"] = "Exclusão do projeto"; 
                model = await api.GetProjetoAsync(model.cd_projeto);
                ViewData["Erro"] = "Essa aplicação não está configurada para acessar a API.";
                return View(model);
            }
        }
    }
}
