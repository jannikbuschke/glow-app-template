using System;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using AutoMapper;
using AutoMapper.EquivalencyExpression;
using Glow.Authentication.Aad;
using Glow.Configurations;
using Glow.TypeScript;
using JannikB.Glue.AspNetCore;
using JannikB.Glue.AspNetCore.Tests;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
        private readonly IWebHostEnvironment env;
        private readonly IConfiguration configuration;

        public Startup(IWebHostEnvironment env, IConfiguration configuration, ILogger<Startup> logger)
        {
            this.env = env;
            this.configuration = configuration;
            logger.LogInformation($"Configuring: '{env.ApplicationName}'");
            logger.LogInformation($"Environment: '{env.EnvironmentName}'");
            logger.LogInformation($"ContentRoot: '{env.ContentRootPath}'");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomAuthentication(env, configuration);
            services.AddCustomAuthorization();

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
                options.UseSqlServer(configuration.GetValue<string>("ConnectionString"));
            });

            services.AddTypescriptGeneration(new[] { Assembly.GetExecutingAssembly() });
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

    public static class StartupExtensions
    {
        public static void AddCustomSpaStaticFiles(this IServiceCollection services)
        {
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "web/build";
            });
        }

        public static void AddCustomMediatR(this IServiceCollection services)
        {
            services.AddMediatR(typeof(Startup).Assembly, typeof(ConfigurationUpdate).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        }

        public static void AddCustomAutoMapper(this IServiceCollection services)
        {
            services.AddAutoMapper(cfg =>
            {
                cfg.AddCollectionMappers();
                //cfg.AddExpressionMapping();
            }, typeof(Startup).Assembly);
        }

        public static void AddCustomSignalR(this IServiceCollection services)
        {
            services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
            //services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();
        }

        public static void AddCustomAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                //options.AddPolicy(Policies.Admin, builder =>
                //{
                //    builder.RequireAuthenticatedUser();
                //    builder.Requirements.Add(new IsAdminRequirement());
                //});

                //options.AddPolicy(Policies.Planner, builder =>
                //{
                //    builder.RequireAuthenticatedUser();
                //    builder.Requirements.Add(new IsPlannerRequirement());
                //});

                //options.AddPolicy(Policies.AuthenticatedUser, builder =>
                //{
                //    builder.RequireAuthenticatedUser();
                //});
            });
        }

        public static void AddCustomAuthentication(
            this IServiceCollection services,
            IWebHostEnvironment env,
            IConfiguration configuration
        )
        {
            services.AddSingleton<TokenService>();
            services.AddSingleton<UserTokenCacheProviderFactory>();
            services.AddSingleton<TicketStoreService>();
            services.AddMemoryCache();
            var testUser = new UserDto { DisplayName = "testuser", Email = "test@sample.com", Id = "1" };
            if (env.IsDevelopment() && configuration.MockExternalSystems())
            {
                services.AddTestAuthentication(
                    testUser.Id,
                    testUser.DisplayName,
                    testUser.Email,
                    new[] {
                        new Claim(ClaimsPrincipalExtensions.ObjectId, testUser.Id),
                        new Claim(ClaimsPrincipalExtensions.TenantId, "our-comp-tenant@id.com")
                    });
            }
            else
            {
                services
                    .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                    .AddAzureAd(options =>
                    {
                        configuration.Bind("OpenIdConnect", options);

                        if (string.IsNullOrEmpty(options.ClientSecret))
                        {
                            options.ClientSecret = configuration["ClientSecret"] ?? throw new Exception("No Clientsecret configured");
                        }
                    });
            }
        }
    }

    public static class IConfigurationExtensions
    {
        public static bool MockExternalSystems(this IConfiguration configuration)
        {
            return configuration.GetValue<bool>("MockDependencies") || configuration.GetValue<bool>("MockExternalSystems");
        }

        public static string GetAppBaseUrl(this IConfiguration configuration)
        {
            return configuration.GetValue<string>("OpenIdConnect:BaseUrl");
        }
    }
}
