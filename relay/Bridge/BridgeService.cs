using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using MTConnectSharp;

namespace mtc_spb_relay.Bridge
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

                OnServiceStart();
                
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
                                    await processFrame(frame);
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

            OnServiceStop();
            _tokenSource.Cancel();
            Task.WaitAny(_task);
            
            return Task.CompletedTask;
        }

        async Task processFrame(MTConnect.ClientServiceChannelFrame frame)
        {
            Console.WriteLine(frame.Type);

            switch (frame.Type)
            {
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.UNKNOWN:

                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.ERROR:
                    await OnMTConnectServiceError(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.PROBE_COMPLETED:
                    await OnMTConnectProbeCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.PROBE_FAILED:
                    await OnMTConnectProbeFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.CURRENT_COMPLETED:
                    await OnMTConnectCurrentCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.CURRENT_FAILED:
                    await OnMTConnectCurrentFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_COMPLETED:
                    await OnMTConnectSampleCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_FAILED:
                    await OnMTConnectSampleFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceChannelFrame.FrameTypeEnum.DATA_CHANGED:
                    await OnMTConnectDataChanged(frame.Payload.client, frame.Payload.xml, frame.Payload.poll);
                    break;
            }
        }

        protected virtual void OnServiceStart()
        {
            
        }

        protected virtual void OnServiceStop()
        {
            
        }

        protected async virtual Task OnMTConnectServiceError(Exception ex)
        {
            Console.WriteLine("ERROR!");
            Console.WriteLine(ex);
        }

        protected async virtual Task OnMTConnectProbeCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            Console.WriteLine("OK: /probe");
        }
        
        protected async virtual Task OnMTConnectProbeFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            Console.WriteLine("ERR: /probe");
            Console.WriteLine(ex);
        }
        
        protected async virtual Task OnMTConnectCurrentCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            Console.WriteLine("OK: /current");
        }
        
        protected async virtual Task OnMTConnectCurrentFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            Console.WriteLine("ERR: /current");
            Console.WriteLine(ex);
        }
        
        protected async virtual Task OnMTConnectSampleCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            Console.WriteLine("OK: /sample");
        }
        
        protected async virtual Task OnMTConnectSampleFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            Console.WriteLine("ERR: /sample");
            Console.WriteLine(ex);
        }
        
        protected async virtual Task OnMTConnectDataChanged(MTConnectSharp.IIMTConnectClient client, XDocument xml, MTConnectClient.SamplePollResult poll)
        {
            Console.WriteLine($"sequence: {poll.StartingSequence} -> {poll.EndingSequence}");
            
            foreach (var kv in poll.DataItems)
            {
                Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
            }
            
            Console.WriteLine("");
        }
    }
}