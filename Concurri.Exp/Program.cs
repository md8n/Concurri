using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Concurri.Exp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseSetting(WebHostDefaults.PreventHostingStartupKey, "true")
                .ConfigureLogging((context, factory) =>
                {
                    factory.AddConfiguration(context.Configuration.GetSection("Logging"));
                    factory.AddConsole();
                    factory.AddDebug();
                })
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
