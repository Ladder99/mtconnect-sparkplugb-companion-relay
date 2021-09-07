using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace mtc_spb_relay
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MTCClient>();
                })
                .RunConsoleAsync();
            
            /*
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();

                var mtc = new MTCClient();
                mtc.Run();
                
                //var spb = new spb_test();
                //await spb.Run();

                Log.Information("Done.");
            }
            catch (Exception ex)
            {
                Log.Error("An exception occurred: {Exception}", ex);
            }
            finally
            {
                Console.ReadKey();
            }
            */
        }
    }
}