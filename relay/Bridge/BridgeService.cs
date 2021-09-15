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
        private ChannelReader<MTConnect.ClientServiceOutboundChannelFrame> _mtcChannelReader;
        private ChannelWriter<MTConnect.ClientServiceInboundChannelFrame> _mtcChannelWriter;
        private ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame> _spbChannelReader;
        private ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame> _spbChannelWriter;
        
        private Task _task1;
        private CancellationTokenSource _tokenSource1;
        private CancellationToken _token1;
        
        private Task _task2;
        private CancellationTokenSource _tokenSource2;
        private CancellationToken _token2;
        
        public BridgeService(
            IHostApplicationLifetime appLifetime,
            ChannelReader<MTConnect.ClientServiceOutboundChannelFrame> mtcChannelReader,
            ChannelWriter<MTConnect.ClientServiceInboundChannelFrame> mtcChannelWriter,
            ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame> spbChannelReader,
            ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame> spbChannelWriter)
        {
            _appLifetime = appLifetime;
            _mtcChannelReader = mtcChannelReader;
            _mtcChannelWriter = mtcChannelWriter;
            _spbChannelReader = spbChannelReader;
            _spbChannelWriter = spbChannelWriter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(() =>
            {
                _tokenSource1 = new CancellationTokenSource();
                _token1 = _tokenSource1.Token;
                _tokenSource2 = new CancellationTokenSource();
                _token2 = _tokenSource2.Token;

                OnServiceStart();
                
                _task1 = Task.Run(async () =>
                {
                    try
                    {
                        while (!_token1.IsCancellationRequested)
                        {
                            while (await _mtcChannelReader.WaitToReadAsync())
                            {
                                await foreach (var frame in _mtcChannelReader.ReadAllAsync())
                                {
                                    await processMtcFrame(frame);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BridgeService MTC ERROR");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Console.WriteLine("BridgeService MTC Stopping");
                        _appLifetime.StopApplication();
                    }
                }, _token1);
                
                _task2 = Task.Run(async () =>
                {
                    try
                    {
                        while (!_token2.IsCancellationRequested)
                        {
                            while (await _spbChannelReader.WaitToReadAsync())
                            {
                                await foreach (var frame in _spbChannelReader.ReadAllAsync())
                                {
                                    await processSpbFrame(frame);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("BridgeService SPB ERROR");
                        Console.WriteLine(ex);
                    }
                    finally
                    {
                        Console.WriteLine("BridgeService SPB Stopping");
                        _appLifetime.StopApplication();
                    }
                }, _token2);
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("BridgeService Stop");

            OnServiceStop();
            _tokenSource1.Cancel();
            _tokenSource2.Cancel();
            Task.WaitAll(_task1, _task2);
            
            return Task.CompletedTask;
        }

        async Task processMtcFrame(MTConnect.ClientServiceOutboundChannelFrame frame)
        {
            Console.WriteLine(frame.Type);

            switch (frame.Type)
            {
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.UNKNOWN:

                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.ERROR:
                    await OnMTConnectServiceError(frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.PROBE_COMPLETED:
                    await OnMTConnectProbeCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.PROBE_FAILED:
                    await OnMTConnectProbeFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.CURRENT_COMPLETED:
                    await OnMTConnectCurrentCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.CURRENT_FAILED:
                    await OnMTConnectCurrentFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.SAMPLE_COMPLETED:
                    await OnMTConnectSampleCompleted(frame.Payload.client, frame.Payload.xml);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.SAMPLE_FAILED:
                    await OnMTConnectSampleFailed(frame.Payload.client, frame.Payload.ex);
                    break;
                
                case MTConnect.ClientServiceOutboundChannelFrame.FrameTypeEnum.DATA_CHANGED:
                    await OnMTConnectDataChanged(frame.Payload.client, frame.Payload.xml, frame.Payload.poll);
                    break;
            }
        }

        async Task processSpbFrame(SparkplugB.ClientServiceOutboundChannelFrame frame)
        {

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