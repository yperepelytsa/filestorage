using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pomelo.EntityFrameworkCore.MySql;
using Pomelo.EntityFrameworkCore.Extensions;
using src.Data;
using src.Services;
using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
using src.Models;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace src
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            
            var configBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false);
            Configuration = configBuilder.Build();
            if (!Directory.Exists("../uploads"))
            {
                Console.WriteLine("Creating uploads folder");
                Directory.CreateDirectory("../uploads");
            }
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            var creds = new Creds()
            {
                username = Configuration["credentials:username"],
                password = Configuration["credentials:password"],
                host = Configuration["credentials:host"],
                
            };
            var defenvironmentid = GetDefaultEnvironment(creds);
            creds.defenvironmentid = defenvironmentid;
            services.AddEntityFrameworkMySql();
            services.AddScoped<DbInitializer>();
            services.AddIdentity<IdentityUser,IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options=>options.UseMySql(Configuration.GetConnectionString("DefaultConnection")));
            services.AddSingleton(typeof(Creds), creds);
            services.AddTransient<IWatsonService, WatsonService>();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseStaticFiles(new StaticFileOptions() {
                FileProvider =new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot"))
            });
            app.UseIdentity();
            app.UseCookieAuthentication();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });
            var dbinit = app.ApplicationServices.GetRequiredService<DbInitializer>();
            dbinit.Initialize();
        }
        private string GetDefaultEnvironment(Creds creds)
        {
            using (var client = new HttpClient())
            {
                var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes(creds.username + ":" + creds.password));
                var req = new HttpRequestMessage(HttpMethod.Get, "/v1/environments");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                req.RequestUri = new Uri("https://gateway.watsonplatform.net/discovery/api/v1/environments?version=2016-12-01");
                var response = client.SendAsync(req).GetAwaiter().GetResult();
                var resstr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                EnvResponse obj = JsonConvert.DeserializeObject<EnvResponse>(resstr);
                var defaultenv = obj.Environments.FirstOrDefault(e => e.Name == "default");
                Console.WriteLine(defaultenv.Environment_id);
                if (defaultenv != null)
                    return defaultenv.Environment_id;
                req = new HttpRequestMessage(HttpMethod.Post, "/v1/environments");
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("name", "default"));
                postData.Add(new KeyValuePair<string, string>("description", "default environment for storage"));
                postData.Add(new KeyValuePair<string, string>("size", "0"));
                req.Content = new FormUrlEncodedContent(postData);
                req.RequestUri = new Uri("https://gateway.watsonplatform.net/discovery/api/v1/environments?version=2016-12-01");
                response = client.SendAsync(req).GetAwaiter().GetResult();
                resstr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                obj = JsonConvert.DeserializeObject<EnvResponse>(resstr);
                return GetDefaultEnvironment(creds);
            }
        }
    }
}
