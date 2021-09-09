using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// async
// https://devblogs.microsoft.com/dotnet/configureawait-faq/

// host
// https://andrewlock.net/introducing-ihostlifetime-and-untangling-the-generic-host-startup-interactions/
// https://dfederm.com/building-a-console-app-with-.net-generic-host/
// https://blog.stephencleary.com/2020/06/backgroundservice-gotcha-application-lifetime.html

// channels
// https://devblogs.microsoft.com/dotnet/an-introduction-to-system-threading-channels/
// https://deniskyashif.com/2019/12/08/csharp-channels-part-1/
// https://flerka.github.io/personal-blog/2020-01-23-communication-with-hosted-service-using-channels/

namespace mtc_spb_relay
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            await Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    /*
                    services.AddHostedService<TerminatorService>();

                    services.AddSingleton(sp => new TerminatorService.TerminatorServiceOptions()
                    {
                        TerminateInMs = 5000
                    });
                    */
                    
                    services.AddHostedService<BridgeService>();
                    
                    services.AddHostedService<SparkplugB.ClientService>();
                    
                    services.AddSingleton(sp => new SparkplugB.ClientServiceOptions()
                    {
                        BrokerAddress = "10.20.30.114",
                        Port = 1883,
                        UseTls = false,
                        Username = "admin",
                        Password = "password",
                        ClientId = Guid.NewGuid().ToString()
                    });
                    
                    services.AddSingleton<Channel<SparkplugB.ClientServiceChannelFrame>>(
                        Channel.CreateUnbounded<SparkplugB.ClientServiceChannelFrame>(
                            new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

                    services.AddSingleton<ChannelWriter<SparkplugB.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceChannelFrame>>().Writer);
                    
                    services.AddSingleton<ChannelReader<SparkplugB.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceChannelFrame>>().Reader);
                    
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
                            new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

                    services.AddSingleton<ChannelWriter<MTConnect.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceChannelFrame>>().Writer);
                    
                    services.AddSingleton<ChannelReader<MTConnect.ClientServiceChannelFrame>>(
                        sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceChannelFrame>>().Reader);
                })
                .RunConsoleAsync();
        }
    }
}