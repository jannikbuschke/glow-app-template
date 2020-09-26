using System.Net;
using AutoMapper;
using AutoMapper.EquivalencyExpression;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TemplateName
{
    public class Startup
    {
        private readonly ILogger<Startup> logger;

        public IConfiguration Configuration { get; }

        public Startup(IWebHostEnvironment env, IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            this.logger = logger;
            logger.LogInformation($"Configuring: '{env.ApplicationName}'");
            logger.LogInformation($"Environment: '{env.EnvironmentName}'");
            logger.LogInformation($"ContentRoot: '{env.ContentRootPath}'");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                });


            services.AddApplicationInsightsTelemetry();

            services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0)
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            services.AddVersionedApiExplorer(o => o.GroupNameFormat = "'v'VVV");

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            services.AddAutoMapper(cfg =>
            {
                cfg.AddCollectionMappers();
            }, typeof(Startup).Assembly);

            services.AddMediatR(typeof(Startup).Assembly);

            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "web/build";
            });

            services.AddDbContext<DataContext>(options =>
            {
                options.UseSqlServer(Configuration.GetValue<string>("ConnectionString"));
            });
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env
        )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseMvc();

            app.Map("/hello", v =>
            {
                v.Run(async ctx =>
                {
                    ctx.Response.StatusCode = (int) HttpStatusCode.OK;
                    await ctx.Response.WriteAsync("hello world");
                });
            });

            app.Map("/api", builder =>
            {
                builder.Run(async context =>
                {
                    context.Response.StatusCode = (int) HttpStatusCode.NotFound;
                    await context.Response.CompleteAsync();
                });
            });

            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "web";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:3000");
                }
            });
        }
    }
}
