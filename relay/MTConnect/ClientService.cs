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
      //private readonly ILogger _logger;
      private readonly IHostApplicationLifetime _appLifetime;
      
      private MTConnectClient _client = null;
      private readonly ClientServiceOptions _serviceOptions;
      private ChannelWriter<ClientServiceChannelFrame> _channelWriter;

      private bool _stopRequested = false;
      
      public ClientService(
         //ILogger<ConsoleHostedService> logger,
         IHostApplicationLifetime appLifetime,
         ClientServiceOptions serviceOptions,
         ChannelWriter<ClientServiceChannelFrame> channelWriter)
      {
         //_logger = logger;
         _appLifetime = appLifetime;
         _serviceOptions = serviceOptions;
         _channelWriter = channelWriter;
      }
      
      public Task StartAsync(CancellationToken cancellationToken)
      {
         _appLifetime.ApplicationStarted.Register(() =>
         {
            Task.Run(async () =>
            {
               try
               {
                  //_logger.LogInformation("Hello World!");

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
                  Console.WriteLine("MTConnect.ClientService ERROR");
                  Console.WriteLine(ex);
                  
                  await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
                  {
                     Type = ClientServiceChannelFrame.FrameTypeEnum.ERROR,
                     Payload = new { ex }
                  });
               }
               finally
               {
                  Console.WriteLine("MTConnect.ClientService Stopping");
                  _channelWriter.Complete();
                  _appLifetime.StopApplication();
               }
            });
         });

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("MTConnect.ClientService Stop");
         _stopRequested = true;
         return Task.CompletedTask;
      }
      
      private async Task _onProbeCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.PROBE_COMPLETED,
            Payload = new { client, xml }
         });
         
         // control OnDataChanged during call to current
         client.SuppressDataItemChangeOnCurrent(_serviceOptions.SupressDataItemChangeOnCurrent);   
            
         await client.GetCurrent();

         while (!_stopRequested)
         {
            var sampleSuccess = await client.GetSample();
            await Task.Delay(sampleSuccess ? _serviceOptions.PollIntervalMs : _serviceOptions.RetryIntervalMs);
         }
      }

      private async Task _onProbeFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.PROBE_FAILED,
            Payload = new { client, ex }
         });
         
         await Task.Delay(_serviceOptions.RetryIntervalMs);
         await client.GetProbe();
      }

      private async Task _onCurrentCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.CURRENT_COMPLETED,
            Payload = new { client, xml }
         });
      }
      
      private async Task _onCurrentFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.CURRENT_FAILED,
            Payload = new { client, ex }
         });
         
         await Task.Delay(_serviceOptions.RetryIntervalMs);
         await client.GetCurrent();
      }
      
      private async Task _onSampleCompleted(IMTConnectClient client, XDocument xml)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_COMPLETED,
            Payload = new { client, xml }
         });
      }
      
      private async Task _onSampleFailed(IMTConnectClient client, Exception ex)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.SAMPLE_FAILED,
            Payload = new { client, ex }
         });
      }

      private async Task _onDataChanged(IMTConnectClient client, XDocument xml, MTConnectClient.SamplePollResult poll)
      {
         await _channelWriter.WriteAsync(new ClientServiceChannelFrame()
         {
            Type = ClientServiceChannelFrame.FrameTypeEnum.DATA_CHANGED,
            Payload = new { client, xml, poll }
         });
      }
   }
}