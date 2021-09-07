using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using MTConnectSharp;

namespace mtc_spb_relay
{
   public class MTCClient: IHostedService
   {
      //private readonly ILogger _logger;
      private readonly IHostApplicationLifetime _appLifetime;

      public MTCClient(
         //ILogger<ConsoleHostedService> logger,
         IHostApplicationLifetime appLifetime)
      {
         //_logger = logger;
         _appLifetime = appLifetime;
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
                     AgentUri = "http://mtconnect.mazakcorp.com:5717",
                     UpdateInterval = TimeSpan.FromSeconds(2)
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
                  // Stop the application once the work is done
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
      
      private MTConnectClient _client = null;
      public string AgentUri { get; private set; }
      public int UpdateInterval { get; private set; }

      private async Task _onProbeCompleted(IMTConnectClient client, XDocument xml)
      {
         Console.WriteLine("OK: /probe");
            
         var items = client.Devices
            .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
            .ToArray();

         // control OnDataChanged during call to current
         client.SuppressDataItemChangeOnCurrent(true);   
            
         await client.GetCurrent();

         while (true)
         {
            await client.GetSample();
            await Task.Delay(2000);
         }
      }

      private async Task _onProbeFailed(IMTConnectClient client, Exception ex)
      {
         Console.WriteLine("ERR: /probe");
         Console.WriteLine(ex);
      }

      private async Task _onCurrentCompleted(IMTConnectClient client, XDocument xml)
      {
         Console.WriteLine("OK: /current");
      }
      
      private async Task _onCurrentFailed(IMTConnectClient client, Exception ex)
      {
         Console.WriteLine("ERR: /current");
         Console.WriteLine(ex);
      }
      
      private async Task _onSampleCompleted(IMTConnectClient client, XDocument xml)
      {
         Console.WriteLine("OK: /sample");
      }
      
      private async Task _onSampleFailed(IMTConnectClient client, Exception ex)
      {
         Console.WriteLine("ERR: /sample");
         Console.WriteLine(ex);
      }

      private async Task _onDataChanged(IMTConnectClient client, XDocument xml, MTConnectClient.SamplePollResult poll)
      {
         // dataitem sample changed
            
         Console.WriteLine($"sequence: {poll.StartingSequence} -> {poll.EndingSequence}");
            
         foreach (var kv in poll.DataItems)
         {
            Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
         }
            
         Console.WriteLine("");
      }
      
      /*public MTCClient(string agentUri = "http://mtconnect.mazakcorp.com:5717", int updateInterval = 1)
      {
         AgentUri = agentUri;
         UpdateInterval = updateInterval;
      }*/
      
      /*
      public async Task Run()
      {
         var client = new MTConnectSharp.MTConnectClient()
         {
            AgentUri = this.AgentUri,
            UpdateInterval = TimeSpan.FromSeconds(this.UpdateInterval)
         };

         client.OnProbeCompleted = async (client, xml) => 
         {
            Console.WriteLine("OK: /probe");
            
            var items = client.Devices
               .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
               .ToArray();

            // control OnDataChanged during call to current
            client.SuppressDataItemChangeOnCurrent(true);   
            
            await client.StartStreaming();
         };

         client.OnProbeFailed = async (client, ex) =>
         {
            Console.WriteLine("ERR: /probe");
            Console.WriteLine(ex);
         };
         
         client.OnCurrentCompleted = async (client, xml) =>
         {
            Console.WriteLine("OK: /current");
         };
         
         client.OnCurrentFailed = async (client, ex) =>
         {
            Console.WriteLine("ERR: /current");
            Console.WriteLine(ex);
         };
         
         client.OnSampleCompleted = async (client, xml) =>
         {
            Console.WriteLine("OK: /sample");
         };
         
         client.OnDataChanged = async (client, xml, poll) =>
         {
            // dataitem sample changed
            
            Console.WriteLine($"sequence: {poll.StartingSequence} -> {poll.EndingSequence}");
            
            foreach (var kv in poll.DataItems)
            {
               Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
            }
            
            Console.WriteLine("");
         };

         await client.GetProbe();
      }
      */
   }
}