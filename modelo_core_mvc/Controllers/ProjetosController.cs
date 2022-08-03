using SefazIdentity.projetos;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SefazIdentity.ProjetosApi;

namespace SefazIdentity.Controllers
{
    [Authorize]
    public class ProjetosController : Controller
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
                await api.PostProjetoAsync(model);
                return RedirectToAction("Index");
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
                await api.PutProjetoAsync(model);
                return RedirectToAction("Index");
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
            if (ModelState.IsValid)
            {
                await api.DeleteProjetoAsync(model.cd_projeto);
                return RedirectToAction("Index");
            }
            return BadRequest();
        }
    }
}
