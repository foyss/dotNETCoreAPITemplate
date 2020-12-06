using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;

namespace FoysCoreAPITemplate
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        public Startup()
        {
            var dom = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", false).AddEnvironmentVariables()
                .Build();

            Configuration = dom;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services.AddHttpContextAccessor(); // net core apps access HttpContext through this accessor via its default implementation
                services.AddCors(); // enable cross origin requests (if client side is on a different host)

                // Kestrel allow synchronous operations
                services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });

                // IIS allow synchronous operations
                services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });

                services.AddControllers().AddJsonOptions(opt =>
                {
                    opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // handle enum validation 
                });

                services.AddControllers();

                // Open ID Server configuration
                var openIdServerConfigs = new OpenIDServerConfiguration();
                openIdServerConfigs.ConfigureOpenIDServer(services);

                services.AddMvc();
                services.AddMvc().AddNewtonsoftJson();


                services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Foys Template API",
                        Version = "v1",
                        Description = "Hello World!"
                    });

                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            try
            {
                if (ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12) == false)
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                if (env.IsDevelopment())
                    app.UseDeveloperExceptionPage();                
                else
                    app.UseExceptionHandler("/error"); // TODO: configure controller action to /error

                //app.UseHttpsRedirection();

                app.UseStaticFiles();
                app.UseAuthentication();
                app.UseRouting();
                app.UseAuthorization();

                app.UseSwagger();
                app.UseReDoc(c =>
                {
                    c.SpecUrl("../swagger/v1/swagger.json");
                    c.RoutePrefix = "docs";
                });

                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("v1/swagger.json", "Foys.Template.API.v1");
                });

                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllerRoute("Foys.Template.API.v1", "{controller=Home}/{action=Index}");
                });
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e);
            }            
        }
    }
}
