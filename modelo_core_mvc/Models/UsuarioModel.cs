using AdaptiveCards;
using AdaptiveCards.Rendering.Html;
using AdaptiveCards.Templating;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text;

namespace modelo_core_mvc.usuarios
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

        public Usuario(string Login, string Nome, string Foto)
        {
            login = Login;
            nome = Nome;
            foto = Foto;
        }

        public Usuario(string Id, string GivenName, string DisplayName, string JobTitle, string Mail, string Photo)
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

        public RenderedAdaptiveCard AdaptiveCard()
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

            //Exemplo copiado de https://docs.microsoft.com/en-us/adaptive-cards/sdk/rendering-cards/net-html/render-a-card
            AdaptiveCardRenderer renderer = new AdaptiveCardRenderer();

            AdaptiveCard card = new AdaptiveCard(renderer.SupportedSchemaVersion)
            {
                Body = { new AdaptiveTextBlock() { Text = textoTemplate } }
            };

            RenderedAdaptiveCard renderedCard = renderer.RenderCard(card);

            return renderedCard;
        }

    }
}