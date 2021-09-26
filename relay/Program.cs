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
                    
                    
                    //services.AddHostedService<Bridge.Example01>();
                    services.AddHostedService<Bridge.Example02>();
                    
                    
                    
                    // SparkplugB.ClientService
                    
                    services.AddHostedService<SparkplugB.ClientService>();
                    
                    services.AddSingleton(sp => new SparkplugB.ClientServiceOptions()
                    {
                        BrokerAddress = "10.20.30.114",
                        BrokerPort = 1883,
                        UseTls = false,
                        Username = "admin",
                        Password = "password",
                        ClientId = Guid.NewGuid().ToString()
                    });
                    
                    
                    // MTConnect.ClientService
                    
                    services.AddHostedService<MTConnect.ClientService>();
                    
                    services.AddSingleton(sp => new MTConnect.ClientServiceOptions()
                    {
                        AgentUri = "http://mtconnect.mazakcorp.com:5717",
                        PollIntervalMs = 2000,
                        RetryIntervalMs = 5000,
                        SupressDataItemChangeOnCurrent = true
                    });
                    
                    
                    // TerminatorService
                    
                    /*
                        How to stop application: Complete the terminator service inbound channel from any service.
                            _tsChannelWriter.Complete();
                    */
                    
                    services.AddHostedService<TerminatorService>();

                    services.AddSingleton(sp => new TerminatorService.TerminatorServiceOptions()
                    {
                        TerminateInMs = 0   // 0 = do not terminate using timer
                    });
                    
                    addChannels(services);
                    
                })
                .RunConsoleAsync();
        }

        static void addChannels(IServiceCollection services)
        {
            // SparkplugB.ClientService
            
            services.AddSingleton<Channel<SparkplugB.ClientServiceOutboundChannelFrame>>(
                Channel.CreateUnbounded<SparkplugB.ClientServiceOutboundChannelFrame>(
                    new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

            services.AddSingleton<ChannelWriter<SparkplugB.ClientServiceOutboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceOutboundChannelFrame>>().Writer);
                    
            services.AddSingleton<ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceOutboundChannelFrame>>().Reader);
                    
            services.AddSingleton<Channel<SparkplugB.ClientServiceInboundChannelFrame>>(
                Channel.CreateUnbounded<SparkplugB.ClientServiceInboundChannelFrame>(
                    new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

            services.AddSingleton<ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceInboundChannelFrame>>().Writer);
                    
            services.AddSingleton<ChannelReader<SparkplugB.ClientServiceInboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<SparkplugB.ClientServiceInboundChannelFrame>>().Reader);
            
            // MTConnect.ClientService
            
            services.AddSingleton<Channel<MTConnect.ClientServiceOutboundChannelFrame>>(
                Channel.CreateUnbounded<MTConnect.ClientServiceOutboundChannelFrame>(
                    new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

            services.AddSingleton<ChannelWriter<MTConnect.ClientServiceOutboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceOutboundChannelFrame>>().Writer);
                    
            services.AddSingleton<ChannelReader<MTConnect.ClientServiceOutboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceOutboundChannelFrame>>().Reader);
                    
            services.AddSingleton<Channel<MTConnect.ClientServiceInboundChannelFrame>>(
                Channel.CreateUnbounded<MTConnect.ClientServiceInboundChannelFrame>(
                    new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));

            services.AddSingleton<ChannelWriter<MTConnect.ClientServiceInboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceInboundChannelFrame>>().Writer);
                    
            services.AddSingleton<ChannelReader<MTConnect.ClientServiceInboundChannelFrame>>(
                sp => sp.GetRequiredService<Channel<MTConnect.ClientServiceInboundChannelFrame>>().Reader);
            
            // TerminatorService
            
            services.AddSingleton<Channel<bool>>(
                Channel.CreateUnbounded<bool>(
                    new UnboundedChannelOptions() { SingleReader = true, SingleWriter = false }));

            services.AddSingleton<ChannelWriter<bool>>(
                sp => sp.GetRequiredService<Channel<bool>>().Writer);
                    
            services.AddSingleton<ChannelReader<bool>>(
                sp => sp.GetRequiredService<Channel<bool>>().Reader);
        }
    }
}