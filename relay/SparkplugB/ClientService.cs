using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

using SparkplugNet.VersionB;
using SparkplugNet.Core.Node;

using VersionBData = SparkplugNet.VersionB.Data;

namespace mtc_spb_relay.SparkplugB
{
   public class ClientService: IHostedService
   {
      private readonly IHostApplicationLifetime _appLifetime;
      private ChannelReader<ClientServiceChannelFrame> _channelReader;
      
      private SparkplugNode _client = null;
      private readonly ClientServiceOptions _serviceOptions;

      private Task _task;
      private CancellationTokenSource _tokenSource;
      private CancellationToken _token;
      
      public ClientService(
         IHostApplicationLifetime appLifetime,
         ClientServiceOptions serviceOptions,
         ChannelReader<ClientServiceChannelFrame> channelReader)
      {
         _appLifetime = appLifetime;
         _serviceOptions = serviceOptions;
         _channelReader = channelReader;
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
                     while (await _channelReader.WaitToReadAsync())
                     {
                        await foreach (var frame in _channelReader.ReadAllAsync())
                        {
                           processFrame(frame);
                        }
                     }
                  }
               }
               catch (Exception ex)
               {
                  Console.WriteLine("SparkplugB.ClientService ERROR");
                  Console.WriteLine(ex);
               }
               finally
               {
                  Console.WriteLine("SparkplugB.ClientService Stopping");
                  _appLifetime.StopApplication();
               }
            }, _token);
         });

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("SparkplugB.ClientService Stop");
         
         _tokenSource.Cancel();
         Task.WaitAny(_task);
         
         return Task.CompletedTask;
      }

      void processFrame(ClientServiceChannelFrame frame)
      {
         Console.WriteLine(frame.Type);

         switch (frame.Type)
         {

         }
      }

      private void CreateNode()
      {
         
      }

      private void CreateDevice()
      {
         
      }
   }
}