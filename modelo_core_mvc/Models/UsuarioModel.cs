using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;

namespace modelo_core_mvc.Usuario
{
    public class Usuarios
    {
        [Display(Name = "Login")]
        public long login { get; set; }
        [Display(Name = "Nome")]
        public string nome { get; set; }
        [Display(Name = "Foto")]
        public string foto { get; set; }
        public Usuarios(long Login, string Nome, string Foto)
        {
            this.login = Login;
            this.nome = Nome;
            this.foto = Foto;
        }

        public StringContent ToJson()
        {
            return new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
        }

        public Usuarios ToModel(string UsuarioJson)
        {
            return JsonConvert.DeserializeObject<Usuarios>(UsuarioJson);
        }

        public IEnumerable<Usuarios> ToList(string UsuarioJson)
        {
            return JsonConvert.DeserializeObject<IEnumerable<Usuarios>>(UsuarioJson);
        }
    }
}