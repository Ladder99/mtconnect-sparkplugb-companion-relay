using System;
using System.Threading.Tasks;

using Serilog;

namespace mtc_spb_relay
{
    class Program
    {
        public static async Task Main()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();

                var mtc = new mtc_test();
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
        }
    }
}