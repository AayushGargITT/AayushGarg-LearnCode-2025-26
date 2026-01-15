
using CountryLookupByCode.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace CountryLookupByCode
{
    public class Program
    {
        static async Task Main()
        {
            using IHost host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<CountryService>();
                    services.AddSingleton<App>();
                })
                .Build();

            var app = host.Services.GetRequiredService<App>();
            await app.RunAsync();
        }
    }
}
