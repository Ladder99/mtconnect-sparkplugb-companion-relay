using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay
{
    public class ChannelReaderService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private ChannelReader<MTConnect.ClientServiceChannelFrame> _channelReader;
        
        public ChannelReaderService(
            IHostApplicationLifetime appLifetime,
            ChannelReader<MTConnect.ClientServiceChannelFrame> channelReader)
        {
            _appLifetime = appLifetime;
            _channelReader = channelReader;
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
                            while (await _channelReader.WaitToReadAsync())
                            {
                                await foreach (var frame in _channelReader.ReadAllAsync())
                                //while (_channel.Reader.TryRead(out var frame))
                                {
                                    processFrame(frame);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ChannelReaderService ERROR");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Console.WriteLine("ChannelReaderService Stopping");
                        _appLifetime.StopApplication();
                    }
                });
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("ChannelReaderService Stop");
            return Task.CompletedTask;
        }

        void processFrame(MTConnect.ClientServiceChannelFrame frame)
        {
            Console.WriteLine(frame.Type);

            switch (frame.Type)
            {
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.UNKNOWN:

                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.ERROR:
                    Console.WriteLine("ERROR!");
                    Console.WriteLine(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.PROBE_COMPLETED:
                    Console.WriteLine("OK: /probe");
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.PROBE_FAILED:
                    Console.WriteLine("ERR: /probe");
                    Console.WriteLine(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.CURRENT_COMPLETED:
                    Console.WriteLine("OK: /current");
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.CURRENT_FAILED:
                    Console.WriteLine("ERR: /current");
                    Console.WriteLine(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_COMPLETED:
                    Console.WriteLine("OK: /sample");
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_FAILED:
                    Console.WriteLine("ERR: /sample");
                    Console.WriteLine(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.DATA_CHANGED:
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