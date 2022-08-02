using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;

namespace modelo_core_mvc.projetos
{
    public class ProjetosModel
    {
        [Display(Name = "Cód")]
        public long cd_projeto { get; set; }
        [Display(Name = "Nome")]
        public string nm_projeto { get; set; }
        [Display(Name = "Descrição")]
        public string ds_projeto { get; set; }
        public ProjetosModel(long cd_projeto, string nm_projeto, string ds_projeto)
        {
            this.cd_projeto = cd_projeto;
            this.nm_projeto = nm_projeto;
            this.ds_projeto = ds_projeto;
        }

        public ProjetosModel()
        {

        }

        public StringContent ToJson()
        {
            return new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
        }

        public ProjetosModel ToModel(string ProjetoJson)
        {
            return JsonConvert.DeserializeObject<ProjetosModel>(ProjetoJson);
        }

        public IEnumerable<ProjetosModel> ToList(string ProjetoJson)
        {
            return JsonConvert.DeserializeObject<IEnumerable<ProjetosModel>>(ProjetoJson);
        }
    }
}
