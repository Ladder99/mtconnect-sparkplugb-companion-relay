using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                    services.AddHostedService<ChannelReaderService>();
                    
                    services.AddHostedService<MTCClient>();
                    
                    services.AddSingleton(sp => new MTCClient.MTCClientOptions()
                    {
                        AgentUri = "http://mtconnect.mazakcorp.com:5717",
                        PollIntervalMs = 2000,
                        RetryIntervalMs = 5000,
                        SupressDataItemChangeOnCurrent = true
                    });
                    
                    services.AddSingleton(Channel.CreateUnbounded<MTCClient.MTCClientChannelPayload>(
                        new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true }));
                })
                .RunConsoleAsync();
        }
    }

    public class ChannelReaderService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private Channel<MTCClient.MTCClientChannelPayload> _channel;
        
        public ChannelReaderService(
            IHostApplicationLifetime appLifetime,
            Channel<MTCClient.MTCClientChannelPayload> channel)
        {
            _appLifetime = appLifetime;
            _channel = channel;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            while (await _channel.Reader.WaitToReadAsync())
                            {
                                while (_channel.Reader.TryRead(out var frame))
                                {
                                    Console.WriteLine(frame.Type);

                                    switch (frame.Type)
                                    {
                                        case MTCClient.MTCClientChannelPayload.PayloadTypeEnum.DATA_CHANGED:
                                            Console.WriteLine($"sequence: {frame.Payload.poll.StartingSequence} -> {frame.Payload.poll.EndingSequence}");
            
                                            foreach (var kv in frame.Payload.poll.DataItems)
                                            {
                                                Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
                                            }
            
                                            Console.WriteLine("");
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        
                    }
                    finally
                    {
                        _appLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}