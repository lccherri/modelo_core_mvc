using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.Threading.Tasks;
using SefazLib.IdentityCfg;
using SefazLib.AzureUtils;
using SefazLib.usuarios;
using modelo_core_mvc.ProjetosApi;
using modelo_core_mvc.Errors;

namespace modelo_core_mvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration configuration;
        private readonly ProjetosApiClient api;
        private readonly AzureUtil mSGraphUtil;

        //Insercao de vulnerabilidades para teste de análise de código
        string username = "teste";
        string password = "123@teste";
        private readonly string[] whiteList = { "https://ads.intra.fazenda.sp.gov.br/tfs" };

        public IActionResult RedirectMe(string url)
        {
            return Redirect(url);
        }
        //Fim do teste

        public HomeController(IConfiguration Configuration, ProjetosApiClient Api, AzureUtil MSGraphUtil)
        {
            configuration = Configuration;
            api = Api;
            mSGraphUtil = MSGraphUtil;
        }

        [Authorize]
        public async Task<IActionResult> Entrar()
        {
            if (configuration["identity:type"] == "azuread")
            {
                Usuario usuario = await mSGraphUtil.GetUserAsync();
                ViewData["html"] = usuario.GetAdaptiveCard().Html;
                ViewData["id"] = usuario.id;
            }
            else
            {
                var claims = User.Claims;
                foreach (var claim in User.Claims)
                {
                    if (claim.Type.Contains("upn"))              { ViewData["Login"]      = claim.Value; }
                    else if (claim.Type.Contains("name"))        { ViewData["Nome"]       = claim.Value; }
                    else if (claim.Type.Contains("CPF"))         { ViewData["Cpf"]        = claim.Value; }
                    else if (claim.Type.Contains("dateofbirth")) { ViewData["Nascimento"] = claim.Value; }
                }
            }
            ViewData["Title"] = "Entrar";

            return View();
        }
        public IActionResult TesteIdentity()
        {
            ViewData["Title"] = "Teste do Identity";
            return View();
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

        public async Task<ActionResult> Sobre()
        {
            ViewData["Title"] = "Sobre";
            ViewData["Message"] = "Sobre essa aplicação";
            ViewData["status"] = await api.GetStatusAsync();
            ViewData["conexao"] = await api.GetConexaoAsync();
            ViewData["EnderecoAPI"] = configuration["apiendereco:projetos"];

            return View();
        }

        [Authorize]
        public async Task<IActionResult> SairAsync()
        {
            ViewData["Title"] = "Sair";
            ViewData["Message"] = "Encerrar a sessão";
            await IdentityConfig.Logout(HttpContext, configuration);

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

    }
}
