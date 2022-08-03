using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using SefazIdentity.projetos;
using System.Diagnostics;
using Microsoft.Identity.Web;
using System.Linq;
using SefazLib.AzureUtils;

namespace SefazIdentity.ProjetosApi
{
    public class ProjetosApiClient
    {
        private readonly HttpClient httpClient;
        private readonly IConfiguration configuration;
        private readonly AzureUtil mSGraphUtil;
        private string accessToken;

        //AzureAD
        public ProjetosApiClient(HttpClient HttpClient, IConfiguration Configuration, AzureUtil MSGraphUtil)
        //public ProjetosApiClient(HttpClient HttpClient, IConfiguration Configuration)
        {
            configuration = Configuration;
            mSGraphUtil = MSGraphUtil;
            httpClient = HttpClient;
            httpClient.BaseAddress = new System.Uri(configuration["apiendereco:projetos"]);

        }

        //Consultar
        public async Task<ProjetosModel> GetProjetoAsync(long cd_projeto)
        {
            await mSGraphUtil.PrepareAuthenticatedClient();
            var resposta = await httpClient.GetAsync($"Projetos/{cd_projeto}");
            resposta.EnsureSuccessStatusCode();
            return new ProjetosModel().ToModel(await resposta.Content.ReadAsStringAsync());
        }

        //Listar todos
        public async Task<IEnumerable<ProjetosModel>> GetProjetosAsync()
        {
            var resposta = await httpClient.GetAsync($"Projetos");
            resposta.EnsureSuccessStatusCode();
            return new ProjetosModel().ToList(await resposta.Content.ReadAsStringAsync());
        }

        //Verificar api
        public async Task<string> GetStatusAsync()
        {
            var resposta = await httpClient.GetAsync($"projetos/status");
            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadAsStringAsync();
        }

        //Verificar conexão
        public async Task<string> GetConexaoAsync()
        {
            var resposta = await httpClient.GetAsync($"projetos/conexao");
            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadAsStringAsync();
        }

        public async Task DeleteProjetoAsync(long cd_projeto)
        {
            if (cd_projeto != 0)
            {
                await mSGraphUtil.PrepareAuthenticatedClient();
                var resposta = await httpClient.DeleteAsync($"Projetos/{cd_projeto}");
                resposta.EnsureSuccessStatusCode();
            }
        }

        //Incluir
        public async Task PostProjetoAsync(ProjetosModel projeto)
        {
            await mSGraphUtil.PrepareAuthenticatedClient();
            var resposta = await httpClient.PostAsync("Projetos", projeto.ToJson());
            resposta.EnsureSuccessStatusCode();
        }

        //Alterar
        public async Task PutProjetoAsync(ProjetosModel projeto)
        {
            await mSGraphUtil.PrepareAuthenticatedClient();
            var resposta = await httpClient.PutAsync("Projetos", projeto.ToJson());
            resposta.EnsureSuccessStatusCode();
        }

        public async Task<byte[]> GetAnexoAsync(long cd_projeto)
        {
            var resposta = await httpClient.GetAsync($"Projetos/{cd_projeto}/anexo");
            resposta.EnsureSuccessStatusCode();
            return await resposta.Content.ReadAsByteArrayAsync();
        }
    }
}
