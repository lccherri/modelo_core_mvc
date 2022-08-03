using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using SefazLib.IdentityCfg;
using SefazLib.AzureUtils;
using SefazIdentity.ProjetosApi;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SefazIdentity.Models;

namespace SefazIdentity
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string[] initialScopes = Configuration.GetValue<string>("CallApi:ScopeForAccessToken")?.Split(' ').ToArray();

            IdentityConfig identityConfig = new IdentityConfig(Configuration);
            var opcoesAutenticacao = identityConfig.AuthenticationOptions;

            switch (Configuration["identity:type"])
            {
                case "azuread":
                    services.AddControllersWithViews().AddMicrosoftIdentityUI();
                    services.AddMicrosoftIdentityWebAppAuthentication(Configuration)
                            .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                            .AddInMemoryTokenCaches();
                    break;
                case ("wsfed"):
                    services.AddControllersWithViews();
                    services.AddAuthentication(opcoesAutenticacao)
                            .AddWsFederation(identityConfig.WSFederationOptions)
                            .AddCookie();
                    break;
                case ("openid"):
                    services.AddControllersWithViews();
                    services.AddAuthentication(opcoesAutenticacao)
                            .AddOpenIdConnect(identityConfig.OpenIdConnectOptions)
                            .AddCookie();
                    break;
                default:
                    services.AddControllersWithViews();
                    services.AddAuthentication(opcoesAutenticacao)
                            .AddWsFederation(identityConfig.WSFederationOptions)
                            .AddCookie("Cookies", identityConfig.CookieAuthenticationOptions);
                    break;
            }

            services.AddApplicationInsightsTelemetry(Configuration["ApplicationInsights:ConnectionString"]);
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();

            services.AddHttpClient<ProjetosApiClient>();
            services.AddTransient<AzureUtil>();
            services.AddTransient<ListModel>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            if (!string.IsNullOrEmpty(Configuration["dadosdeploy:nomeappk8s"]))
            {
                //No servidor kubernetes com aplicações compartilhadas, a pasta base da rota deve ser informada (nomeappk8s)
                app.Use((context, next) =>
                {
                    context.Request.PathBase = "/" + Configuration["dadosdeploy:nomeappk8s"];
                    return next();
                });
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
