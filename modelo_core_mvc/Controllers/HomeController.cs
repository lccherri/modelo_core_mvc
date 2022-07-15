using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using modelo_core_mvc.Usuario;
using modelo_core_mvc.HttpClients;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using modelo_core_mvc.Identity;

namespace modelo_core_mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly ProjetosApiClient _api;

        //Insercao de vulnerabilidades para teste de análise de código
        string username = "teste";
        string password = "123@teste";
        private readonly string[] whiteList = { "https://ads.intra.fazenda.sp.gov.br/tfs" };

        public IActionResult RedirectMe(string url)
        {
            return Redirect(url);
        }
        //Fim do teste

        public HomeController(IConfiguration configuration, ProjetosApiClient api)
        {
            Configuration = configuration;
            _api = api;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacidade()
        {
            ViewData["Title"] = "Privacidade";
            return View();
        }

        public IActionResult Contato()
        {
            ViewData["Title"] = "Contato";
            return View();
        }

        public IActionResult TesteIdentity()
        {
            var claims = User.Claims;
            ViewData["Title"] = "Teste do Identity";
            return View();
        }

        public async Task<ActionResult> Sobre()
        {
            ViewData["Title"] = "Sobre";
            ViewData["Message"] = "Sobre essa aplicação";
            ViewData["status"] = await _api.GetStatusAsync();
            ViewData["conexao"] = await _api.GetConexaoAsync();
            ViewData["EnderecoAPI"] = Configuration["apiendereco:projetos"];

            return View();
        }

        [Authorize]
        public async Task<IActionResult> SairAsync()
        {
            ViewData["Title"] = "Sair";
            ViewData["Message"] = "Encerrar a sessão";
            await IdentityConfig.Logout(HttpContext);

            return View();
        }

        [Authorize]
        public IActionResult Entrar()
        {
            ViewData["Title"] = "Entrar";

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
