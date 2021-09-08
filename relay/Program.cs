using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// https://andrewlock.net/introducing-ihostlifetime-and-untangling-the-generic-host-startup-interactions/
// https://deniskyashif.com/2019/12/08/csharp-channels-part-1/
// https://dfederm.com/building-a-console-app-with-.net-generic-host/
// https://flerka.github.io/personal-blog/2020-01-23-communication-with-hosted-service-using-channels/
// https://blog.stephencleary.com/2020/06/backgroundservice-gotcha-application-lifetime.html

namespace mtc_spb_relay
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<TerminatorService>();

                    services.AddSingleton(sp => new TerminatorService.TerminatorServiceOptions()
                    {
                        TerminateInMs = 10000
                    });
                    
                    services.AddHostedService<ChannelReaderService>();
                    
                    services.AddHostedService<MTConnect.ClientService>();
                    
                    services.AddSingleton(sp => new MTConnect.ClientServiceOptions()
                    {
                        AgentUri = "http://mtconnect.mazakcorp.com:5717",
                        PollIntervalMs = 2000,
                        RetryIntervalMs = 5000,
                        SupressDataItemChangeOnCurrent = true
                    });
                    
                    services.AddSingleton<Channel<MTConnect.ClientServiceChannelFrame>>(
                        Channel.CreateUnbounded<MTConnect.ClientServiceChannelFrame>(
                            new UnboundedChannelOptions() { SingleReader = false, SingleWriter = true }));

                    services.AddSingleton<ChannelWriter<MTConnect.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceChannelFrame>>().Writer);
                    
                    services.AddSingleton<ChannelReader<MTConnect.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceChannelFrame>>().Reader);
                })
                .RunConsoleAsync();
        }
    }
}