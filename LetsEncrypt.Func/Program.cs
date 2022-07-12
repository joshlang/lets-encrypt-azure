using Azure.Core;
using Azure.Identity;
using LetsEncrypt.Func.Config;
using LetsEncrypt.Logic;
using LetsEncrypt.Logic.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;

namespace LetsEncrypt.Func;

public static class Program
{
    public static async Task Main()
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults()
            .ConfigureServices((ctx, s) =>
            {
                s.AddLogging();

                var configuration = ctx.Configuration;
                // internal storage (used for letsencrypt account metadata)
                s.AddSingleton<IStorageProvider>(new AzureBlobStorageProvider(configuration["AzureWebJobsStorage"], "letsencrypt"));

                s.AddSingleton<TokenCredential, DefaultAzureCredential>();
                s.Scan(scan =>
                    scan.FromAssemblyOf<RenewalService>()
                        .AddClasses()
                        .AsMatchingInterface()
                        .WithTransientLifetime()
                        .FromAssemblyOf<ConfigurationLoader>()
                        .AddClasses()
                        .AsMatchingInterface()
                        .WithTransientLifetime());
            })
            .Build();

        await host.RunAsync();
    }
}
