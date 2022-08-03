using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;

namespace modelo_core_mvc.Models
{
    public class ListModel
    {
        [Display(Name = "Cargo")]
        public string coluna1 { get; set; }
        [Display(Name = "teste")]
        public string coluna2 { get; set; }

        public ListModel(string Coluna1, string Coluna2)
        {
            coluna1 = Coluna1;
            coluna2 = Coluna2;
        }

        public ListModel()
        {
        }

        public StringContent ToJson()
        {
            return new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
        }

        public ListModel ToModel(string MSListJson)
        {
            return JsonConvert.DeserializeObject<ListModel>(MSListJson);
        }

        public IEnumerable<ListModel> ToList(string MSListJson)
        {
            return JsonConvert.DeserializeObject<IEnumerable<ListModel>>(MSListJson);
        }
    }
}
