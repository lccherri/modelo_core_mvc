using AdaptiveCards;
using AdaptiveCards.Rendering.Html;
using AdaptiveCards.Templating;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;

namespace SefazLib.usuarios
{
    public class Usuario
    {
        [Display(Name = "Login")]
        public string login { get; set; }
        [Display(Name = "Nome")]
        public string nome { get; set; }
        [Display(Name = "Foto")]
        public string foto { get; set; }

        //Azure properties
        public string nomeCompleto { get; set; }
        public string cargo { get; }
        public string email { get; }
        public string id { get; }
        public string graphToken;

        public Usuario(string Login, string Nome, string Foto)
        {
            login = Login;
            nome = Nome;
            foto = Foto;
        }

        public Usuario(string Id, string GivenName, string DisplayName, string JobTitle, string Mail, string Photo, string graphToken)
        {
            id = Id;
            nomeCompleto = DisplayName;
            nome = GivenName;
            cargo = JobTitle;
            email = Mail;
            login = Mail.Split('@')[0];
            foto = Photo;
        }

        public Usuario()
        {
        }

        public StringContent ToJson()
        {
            return new StringContent(JsonConvert.SerializeObject(this), Encoding.UTF8, "application/json");
        }

        public Usuario ToModel(string UsuarioJson)
        {
            return JsonConvert.DeserializeObject<Usuario>(UsuarioJson);
        }

        public IEnumerable<Usuario> ToList(string UsuarioJson)
        {
            return JsonConvert.DeserializeObject<IEnumerable<Usuario>>(UsuarioJson);
        }

        public RenderedAdaptiveCard GetAdaptiveCard()
        {
            var templateJson = @"
                                 {
                                     ""type"": ""AdaptiveCard"",
                                     ""body"": [
                                         {
                                             ""type"": ""TextBlock"",
                                             ""size"": ""large"",
                                             ""weight"": ""bolder"",
                                             ""text"": ""${nomecompleto}""
                                         },
                                         {
                                             ""type"": ""FactSet"",
                                             ""facts"": [
                                                 {
                                                     ""title"": ""Nome"",
                                                     ""value"": ""${nome}""
                                                 },
                                                 {
                                                     ""title"": ""Nome completo"",
                                                     ""value"": ""${nomecompleto}""
                                                 },
                                                 {
                                                     ""title"": ""Cargo"",
                                                     ""value"": ""${cargo}""
                                                 },
                                                 {
                                                     ""title"": ""Login"",
                                                     ""value"": ""${login}""
                                                 },
                                                 {
                                                     ""title"": ""email"",
                                                     ""value"": ""${email}""
                                                 },
                                                 {
                                                     ""title"": ""id"",
                                                     ""value"": ""${id}""
                                                 }
                                             ]
                                         }
                                     ],
                                     ""$schema"": ""http://adaptivecards.io/schemas/adaptive-card.json"",
                                     ""version"": ""1.0""
                                 }
                                ";

            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJson);
            var textoTemplate = template.Expand(this);

            var jObject = JObject.Parse(textoTemplate);
            if (!jObject.TryGetValue("version", out var _))
                jObject["version"] = "0.5";


            // Parse the Adaptive Card JSON
            AdaptiveCardParseResult parseResult = AdaptiveCard.FromJson(jObject.ToString());
            AdaptiveCard card = parseResult.Card;

            AdaptiveCardRenderer renderer = new();
            RenderedAdaptiveCard renderedCard = renderer.RenderCard(card);
            var html = renderedCard.Html;

            return renderedCard;
        }

    }
}