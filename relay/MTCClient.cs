using System;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using MTConnectSharp;

namespace mtc_spb_relay
{
   public class MTCClient: IHostedService
   {
      public class MTCClientChannelPayload
      {
         public enum PayloadTypeEnum
         {
            UNKNOWN,
            PROBE_COMPLETED,
            PROBE_FAILED,
            CURRENT_COMPLETED,
            CURRENT_FAILED,
            SAMPLE_COMPLETED,
            SAMPLE_FAILED,
            DATA_CHANGED
         }

         public PayloadTypeEnum Type { get; set; }
         public dynamic Payload { get; set; }
      }
      
      public class MTCClientOptions
      {
         public string AgentUri { get; set; }
         public int PollIntervalMs { get; set; }
         public int RetryIntervalMs { get; set; }
         public bool SupressDataItemChangeOnCurrent { get; set; }
      }
      
      //private readonly ILogger _logger;
      private readonly IHostApplicationLifetime _appLifetime;
      
      private MTConnectClient _client = null;
      private readonly MTCClientOptions _options;
      private Channel<MTCClientChannelPayload> _channel;

      private bool _stopRequested = false;
      
      public MTCClient(
         //ILogger<ConsoleHostedService> logger,
         IHostApplicationLifetime appLifetime,
         MTCClientOptions options,
         Channel<MTCClientChannelPayload> channel)
      {
         //_logger = logger;
         _appLifetime = appLifetime;
         _options = options;
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
                  //_logger.LogInformation("Hello World!");

                  _client = new MTConnectClient()
                  {
                     AgentUri = _options.AgentUri,
                     UpdateInterval = TimeSpan.FromMilliseconds(_options.PollIntervalMs)
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
                  //_logger.LogError(ex, "Unhandled exception!");
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
         _stopRequested = true;
         return Task.CompletedTask;
      }
      
      private async Task _onProbeCompleted(IMTConnectClient client, XDocument xml)
      {
         //Console.WriteLine("OK: /probe");

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.PROBE_COMPLETED,
               Payload = new { client, xml }
            });
            //_channel.Writer.TryComplete();
         }
         /*
          var items = client.Devices
            .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
            .ToArray();
         */
         
         // control OnDataChanged during call to current
         client.SuppressDataItemChangeOnCurrent(_options.SupressDataItemChangeOnCurrent);   
            
         await client.GetCurrent();

         while (!_stopRequested)
         {
            var sampleSuccess = await client.GetSample();
            await Task.Delay(sampleSuccess ? _options.PollIntervalMs : _options.RetryIntervalMs);
         }
      }

      private async Task _onProbeFailed(IMTConnectClient client, Exception ex)
      {
         /*
         Console.WriteLine("ERR: /probe");
         Console.WriteLine(ex);
         */

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.PROBE_FAILED,
               Payload = new { client, ex }
            });
            //_channel.Writer.TryComplete();
         }

         await Task.Delay(_options.RetryIntervalMs);
         await client.GetProbe();
      }

      private async Task _onCurrentCompleted(IMTConnectClient client, XDocument xml)
      {
         //Console.WriteLine("OK: /current");

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.CURRENT_COMPLETED,
               Payload = new { client, xml }
            });
            //_channel.Writer.TryComplete();
         }
      }
      
      private async Task _onCurrentFailed(IMTConnectClient client, Exception ex)
      {
         /*
         Console.WriteLine("ERR: /current");
         Console.WriteLine(ex);
         */

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.CURRENT_FAILED,
               Payload = new { client, ex }
            });
            //_channel.Writer.TryComplete();
         }

         await Task.Delay(_options.RetryIntervalMs);
         await client.GetCurrent();
      }
      
      private async Task _onSampleCompleted(IMTConnectClient client, XDocument xml)
      {
         //Console.WriteLine("OK: /sample");

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.SAMPLE_COMPLETED,
               Payload = new { client, xml }
            });
            //_channel.Writer.TryComplete();
         }
      }
      
      private async Task _onSampleFailed(IMTConnectClient client, Exception ex)
      {
         /*
         Console.WriteLine("ERR: /sample");
         Console.WriteLine(ex);
         */

         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.SAMPLE_FAILED,
               Payload = new { client, ex }
            });
            //_channel.Writer.TryComplete();
         }
      }

      private async Task _onDataChanged(IMTConnectClient client, XDocument xml, MTConnectClient.SamplePollResult poll)
      {
         if (_channel != null)
         {
            await _channel.Writer.WriteAsync(new MTCClientChannelPayload()
            {
               Type = MTCClientChannelPayload.PayloadTypeEnum.DATA_CHANGED,
               Payload = new { client, xml, poll }
            });
            //_channel.Writer.TryComplete();
         }

         /*
         Console.WriteLine($"sequence: {poll.StartingSequence} -> {poll.EndingSequence}");
            
         foreach (var kv in poll.DataItems)
         {
            Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
         }
            
         Console.WriteLine("");
         */
      }
   }
}