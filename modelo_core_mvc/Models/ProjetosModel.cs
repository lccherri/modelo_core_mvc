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
        public long id { get; set; }
        [Display(Name = "Nome")]
        public string nome { get; set; }
        [Display(Name = "Descrição")]
        public string descricao { get; set; }
        public ProjetosModel(long Id, string Nome, string Descricao)
        {
            id = Id;
            nome = Nome;
            descricao = Descricao;
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
