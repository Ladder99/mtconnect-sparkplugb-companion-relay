using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using MTConnectSharp;

namespace mtc_spb_relay.MTConnect
{
   public class ClientService: IHostedService
   {
      private readonly IHostApplicationLifetime _appLifetime;
      
      private MTConnectClient _client = null;
      private readonly ClientServiceOptions _serviceOptions;
      private ChannelReader<ClientServiceInboundChannelFrame> _channelReader;
      private ChannelWriter<ClientServiceOutboundChannelFrame> _channelWriter;

      private Task _task1;
      private CancellationTokenSource _tokenSource1;
      private CancellationToken _token1;
      
      private Task _task2;
      private CancellationTokenSource _tokenSource2;
      private CancellationToken _token2;
      
      public ClientService(
         IHostApplicationLifetime appLifetime,
         ClientServiceOptions serviceOptions,
         ChannelReader<ClientServiceInboundChannelFrame> channelReader,
         ChannelWriter<ClientServiceOutboundChannelFrame> channelWriter)
      {
         _appLifetime = appLifetime;
         _serviceOptions = serviceOptions;
         _channelReader = channelReader;
         _channelWriter = channelWriter;
      }
      
      public Task StartAsync(CancellationToken cancellationToken)
      {
         _appLifetime.ApplicationStarted.Register(() =>
         {
            _tokenSource1 = new CancellationTokenSource();
            _token1 = _tokenSource1.Token;
            
            _tokenSource2 = new CancellationTokenSource();
            _token2 = _tokenSource2.Token;
            
            _task1 = Task.Run(async () =>
            {
               try
               {
                  _client = new MTConnectClient()
                  {
                     AgentUri = _serviceOptions.AgentUri,
                     UpdateInterval = TimeSpan.FromMilliseconds(_serviceOptions.PollIntervalMs)
                  };

                  _client.OnProbeCompleted = this._onProbeCompleted;
                  _client.OnProbeFailed = this._onProbeFailed;
                  _client.OnCurrentCompleted = this._onCurrentCompleted;
                  _client.OnCurrentFailed = this._onCurrentFailed;
                  _client.OnSampleCompleted = this._onSampleCompleted;
                  _client.OnSampleFailed = this._onSampleFailed;
                  _client.OnDataChanged = this._onDataChanged;

                  await _client.GetProbe();
               }
               catch (Exception ex)
               {
                  Console.WriteLine("MTConnect.ClientService CLIENT ERROR");
                  Console.WriteLine(ex);
                  
                  await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
                  {
                     Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.ERROR,
                     Payload = new { ex }
                  });
               }
               finally
               {
                  Console.WriteLine("MTConnect.ClientService CLIENT Stopping");
                  _appLifetime.StopApplication();
               }
            }, _token1);
            
            _task2 = Task.Run(async () =>
            {
               try
               {
                  while (!_token2.IsCancellationRequested)
                  {
                     while (await _channelReader.WaitToReadAsync())
                     {
                        await foreach (var frame in _channelReader.ReadAllAsync())
                        {
                           await processMtcFrame(frame);
                        }
                     }
                  }
               }
               catch (Exception ex)
               {
                  Console.WriteLine("MTConnect.ClientService CHANNEL_READER ERROR");
                  Console.WriteLine(ex);
               }
               finally
               {
                  Console.WriteLine("MTConnect.ClientService CHANNEL_READER Stopping");
                  _appLifetime.StopApplication();
               }
            }, _token2);
         });

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("MTConnect.ClientService Stop");
         
         _tokenSource1.Cancel();
         _tokenSource2.Cancel();
         Task.WaitAll(_task1, _task2);
         
         return Task.CompletedTask;
      }

      async Task processMtcFrame(ClientServiceInboundChannelFrame frame)
      {

      }

      private async Task _onProbeCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.PROBE_COMPLETED,
            Payload = new { client, xml }
         });
         
         // control OnDataChanged during call to current
         client.SuppressDataItemChangeOnCurrent(_serviceOptions.SupressDataItemChangeOnCurrent);   
            
         await client.GetCurrent();

         while (!_token1.IsCancellationRequested)
         {
            var sampleSuccess = await client.GetSample();
            await Task.Delay(sampleSuccess ? _serviceOptions.PollIntervalMs : _serviceOptions.RetryIntervalMs);
         }
      }

      private async Task _onProbeFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.PROBE_FAILED,
            Payload = new { client, ex }
         });
         
         await Task.Delay(_serviceOptions.RetryIntervalMs);
         await client.GetProbe();
      }

      private async Task _onCurrentCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.CURRENT_COMPLETED,
            Payload = new { client, xml }
         });
      }
      
      private async Task _onCurrentFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.CURRENT_FAILED,
            Payload = new { client, ex }
         });
         
         await Task.Delay(_serviceOptions.RetryIntervalMs);
         await client.GetCurrent();
      }
      
      private async Task _onSampleCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.SAMPLE_COMPLETED,
            Payload = new { client, xml }
         });
      }
      
      private async Task _onSampleFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.SAMPLE_FAILED,
            Payload = new { client, ex }
         });
      }

      private async Task _onDataChanged(IMTConnectClient client, XDocument xml, MTConnectClient.SamplePollResult poll)
      {
         await _channelWriter.WriteAsync(new ClientServiceOutboundChannelFrame()
         {
            Type = ClientServiceOutboundChannelFrame.FrameTypeEnum.DATA_CHANGED,
            Payload = new { client, xml, poll }
         });
      }
   }
}