using System;
using System.Collections.Generic;
using System.Linq;
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
      private ChannelWriter<bool> _tsChannelWriter;
      
      private SparkplugNode _client = null;

      private readonly CancellationTokenSource CancellationTokenSource = new ();
      
      private Task _task1;
      private CancellationTokenSource _tokenSource1;
      private CancellationToken _token1;
      
      private Task _task2;
      private CancellationTokenSource _tokenSource2;
      private CancellationToken _token2;
      
      public ClientService(
         IHostApplicationLifetime appLifetime,
         ChannelReader<ClientServiceInboundChannelFrame> channelReader,
         ChannelWriter<ClientServiceOutboundChannelFrame> channelWriter,
         ChannelWriter<bool> tsChannelWriter)
      {
         _appLifetime = appLifetime;
         _channelReader = channelReader;
         _channelWriter = channelWriter;
         _tsChannelWriter = tsChannelWriter;
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
                           await processFrame(frame);
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

      async Task processFrame(ClientServiceInboundChannelFrame frame)
      {
         Console.WriteLine(frame.Type);

         switch (frame.Type)
         {
            case ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_BIRTH:
               var nodeBirthMetrics = (frame.Payload.data as List<dynamic>)?
                  .ConvertAll<SparkplugNet.VersionB.Data.Metric>(data => frame.Payload.mapper(data));

               await CreateNode(frame.Payload.options, nodeBirthMetrics, frame.Payload.groupId, frame.Payload.nodeId);
               break;
            
            case ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DATA:
               var nodeMetrics = (frame.Payload.data as List<dynamic>)?
                  .ConvertAll<SparkplugNet.VersionB.Data.Metric>(data => frame.Payload.mapper(data));

               await UpdateNode(nodeMetrics);
               break;
            
            case ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DEATH:
               await KillNode();
               break;
            
            case ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_BIRTH:
               var deviceBirthMetrics = (frame.Payload.data as List<dynamic>)?
                  .ConvertAll<SparkplugNet.VersionB.Data.Metric>(data => frame.Payload.mapper(data));

               await CreateDevice(frame.Payload.deviceId, deviceBirthMetrics);
               break;
            
            case ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DATA:
               var deviceMetrics = (frame.Payload.data as List<dynamic>)?
                  .ConvertAll<SparkplugNet.VersionB.Data.Metric>(data => frame.Payload.mapper(data));

               await CreateDevice(frame.Payload.deviceId, deviceMetrics);
               break;
            
            case ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DEATH:
               KillDevice(frame.Payload.deviceId);
               break;
         }
      }

      async Task CreateNode(ClientServiceOptions options, List<VersionBData.Metric> metrics, string groupId, string nodeId)
      {
         if (_client == null)
         {
            _client = new SparkplugNode(metrics);
         }

         if (!_client.IsConnected)
         {
            var nodeOptions = new SparkplugNodeOptions(
               options.BrokerAddress,
               options.BrokerPort,
               options.ClientId,
               options.Username,
               options.Password,
               options.UseTls,
               "scada1",
               groupId,
               nodeId,
               options.ReconnectInterval,
               null,
               null,
               CancellationTokenSource.Token);

            await _client.Start(nodeOptions);
         }
      }

      async Task UpdateNode(List<VersionBData.Metric> metrics)
      {
         if (_client != null)
         {
            if (_client.IsConnected)
            {
               await _client.PublishMetrics(metrics); 
            }
         }
      }
      
      async Task KillNode()
      {
         if (_client != null)
         {
            if (_client.IsConnected)
            {
               await _client.Stop();  
            }
         }
      }

      async Task CreateDevice(string id, List<VersionBData.Metric> metrics)
      {
         if (_client != null)
         {
            if (_client.IsConnected)
            {
               await _client.PublishDeviceBirthMessage(metrics, id);
            }
         }
      }

      async Task UpdateDevice(string id, List<VersionBData.Metric> metrics)
      {
         if (_client != null)
         {
            if (_client.IsConnected)
            {
               await _client.PublishDeviceData(metrics, id);
            }
         }
      }

      async Task KillDevice(string id)
      {
         if (_client != null)
         {
            if (_client.IsConnected)
            {
               await _client.PublishDeviceDeathMessage(id);
            }
         }
      }
   }
}