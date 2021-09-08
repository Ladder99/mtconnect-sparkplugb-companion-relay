using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay
{
    public class TerminatorService : IHostedService
    {
        public class TerminatorServiceOptions
        {
            public int TerminateInMs
            {
                get;
                set;
            }
        }
        
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly TerminatorServiceOptions _options;
        
        public TerminatorService(
            IHostApplicationLifetime appLifetime,
            TerminatorServiceOptions options)
        {
            _appLifetime = appLifetime;
            _options = options;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(_options.TerminateInMs);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("TerminatorService ERROR");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Console.WriteLine("TerminatorService Stopping");
                        _appLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TerminatorService Stop");
            return Task.CompletedTask;
        }
    }
}