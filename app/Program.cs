using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace TemplateName
{
    public class Program
    {
        private static string EnvironmentName
        {
            get
            {
                return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            }
        }

        public static IConfiguration Configuration
        {
            get
            {
                return new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{EnvironmentName}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build();
            }
        }

        private static Logger GetPreStartLogger()
        {
            return EnvironmentName == "Production"
                ? new LoggerConfiguration()
                    .WriteTo.RollingFile("logs/log-start-{Date}.txt")
                    .CreateLogger()
                : new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
        }

        public static int Main(string[] args)
        {
            Log.Logger = GetPreStartLogger();
            string name = typeof(Program).Namespace;
            Log.Information($"Starting {name}");

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", typeof(Program).Namespace)
                    .CreateLogger();

                Log.Information($"Build host {name}");
                IWebHost host = CreateWebHostBuilder(args).Build();

                using (IServiceScope scope = host.Services.CreateScope())
                {
                    DataContext db = scope.ServiceProvider.GetRequiredService<DataContext>();
                    db = scope.ServiceProvider.GetRequiredService<DataContext>();
                    if (EnvironmentName == "Development" || EnvironmentName == "Test")
                    {
                        //db.Database.EnsureDeleted();
                        db.Database.Migrate();
                    }
                    else
                    {
                        db.Database.Migrate();
                    }
                }

                host.Run();
                return 0;

            }
            catch (Exception e)
            {
                Log.Fatal(e, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureAppConfiguration((context, config) =>
                {
                    IConfigurationRoot cfg = config.Build();
                    string name = cfg.GetValue<string>("KeyVaultName");

                    if (context.HostingEnvironment.IsProduction() && !string.IsNullOrEmpty(name))
                    {
                        string keyvaultDns = $"https://{name}.vault.azure.net/";
                        Log.Information("Using KeyVault {KeyVault}", keyvaultDns);
                        AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                        KeyVaultClient keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
                        config.AddAzureKeyVault(keyvaultDns, keyVaultClient, new DefaultKeyVaultSecretManager());
                    }
                    else
                    {
                        Log.Information("No KeyVault configured");
                    }
                })
                .UseConfiguration(Configuration)
                .UseSerilog();
        }
    }
}
