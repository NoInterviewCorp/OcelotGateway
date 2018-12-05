using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http;
using Consul;
using Chilkat;
using Ocelot.Provider.Consul;

namespace GatewayApi
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddOcelot(Configuration).AddConsul();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                //                app.UseHsts();
            }

            app.UseCors(x => x
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                );


            app.Use(async (context, next) =>
            {
                Console.WriteLine(context.Request.Path.Value);
                if (context.Request.Path.Value=="/" || context.Request.Path.Value == "/auth" || context.Request.Path.Value.Contains("/signIn") || context.Request.Path.Value.Contains("/signUp") || context.Request.Path.Value.Contains("/socialSignIn"))
                {
                    await next();
                }
                else
                {
                    Microsoft.AspNetCore.Http.IRequestCookieCollection cookies = context.Request.Cookies;
                    var token = cookies["TOKEN"];
                    // var token = context.Request.Cookies["TOKEN"] ;
                    // var token = context.Request.Headers["Authorization"];
                    Chilkat.Global glob = new Chilkat.Global();
                    glob.UnlockBundle("Anything for 30-day trial");

                    using (var client = new ConsulClient())
                    {
                        // string ConsulIpHost = "http://consul:8500";
                        // client.Config.Address = new Uri(ConsulIpHost);
                        client.Config.Address = new Uri("http://172.23.238.173:8500");
                        var getpair2 = client.KV.Get("myPublicKey");
                        string secret = System.Text.Encoding.UTF8.GetString(getpair2.Result.Response.Value);
                        Chilkat.Rsa rsaExportedPublicKey = new Chilkat.Rsa();
                        rsaExportedPublicKey.ImportPublicKey(secret);
                        var publickey = rsaExportedPublicKey.ExportPublicKeyObj();
                        var jwt = new Chilkat.Jwt();
                        if (jwt.VerifyJwtPk(token, publickey) && (jwt.IsTimeValid(token, 0)))
                        {
                            await next();
                        }
                        else
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("UnAuthorized");
                        }
                    }
                }
            });

            //  app.UseHttpsRedirection();
            //          app.UseMvc();
            await app.UseOcelot();
        }
    }
}
