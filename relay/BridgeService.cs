using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay
{
    public class BridgeService : IHostedService
    {
        private readonly IHostApplicationLifetime _appLifetime;
        private ChannelReader<MTConnect.ClientServiceChannelFrame> _mtcChannelReader;
        private ChannelWriter<SparkplugB.ClientServiceChannelFrame> _spbChannelWriter;
        
        private Task _task;
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        
        public BridgeService(
            IHostApplicationLifetime appLifetime,
            ChannelReader<MTConnect.ClientServiceChannelFrame> mtcChannelReader,
            ChannelWriter<SparkplugB.ClientServiceChannelFrame> spbChannelWriter)
        {
            _appLifetime = appLifetime;
            _mtcChannelReader = mtcChannelReader;
            _spbChannelWriter = spbChannelWriter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                _tokenSource = new CancellationTokenSource();
                _token = _tokenSource.Token;
                
                _task = Task.Run(async () =>
                {
                    try
                    {
                        while (!_token.IsCancellationRequested)
                        {
                            while (await _mtcChannelReader.WaitToReadAsync())
                            {
                                await foreach (var frame in _mtcChannelReader.ReadAllAsync())
                                {
                                    processFrame(frame);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BridgeService ERROR");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Console.WriteLine("BridgeService Stopping");
                        _appLifetime.StopApplication();
                    }
                }, _token);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("BridgeService Stop");
            
            _tokenSource.Cancel();
            Task.WaitAny(_task);
            
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