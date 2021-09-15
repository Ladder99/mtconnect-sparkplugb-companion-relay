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
      private ChannelReader<ClientServiceInboundChannelFrame> _channelReader;
      private ChannelWriter<ClientServiceOutboundChannelFrame> _channelWriter;
      
      private SparkplugNode _client = null;
      private readonly ClientServiceOptions _serviceOptions;

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
                  while (!_token1.IsCancellationRequested)
                  {
                     await Task.Yield();
                  }
               }
               catch (Exception ex)
               {
                  Console.WriteLine("SparkplugB.ClientService CLIENT ERROR");
                  Console.WriteLine(ex);
               }
               finally
               {
                  Console.WriteLine("SparkplugB.ClientService CLIENT Stopping");
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
                           processFrame(frame);
                        }
                     }
                  }
               }
               catch (Exception ex)
               {
                  Console.WriteLine("SparkplugB.ClientService CHANNEL_READER ERROR");
                  Console.WriteLine(ex);
               }
               finally
               {
                  Console.WriteLine("SparkplugB.ClientService CHANNEL_READER Stopping");
                  _appLifetime.StopApplication();
               }
            }, _token2);
         });

         return Task.CompletedTask;
      }

      public Task StopAsync(CancellationToken cancellationToken)
      {
         Console.WriteLine("SparkplugB.ClientService Stop");
         
         _tokenSource1.Cancel();
         _tokenSource2.Cancel();
         Task.WaitAll(_task1, _task2);
         
         return Task.CompletedTask;
      }

      void processFrame(ClientServiceInboundChannelFrame frame)
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