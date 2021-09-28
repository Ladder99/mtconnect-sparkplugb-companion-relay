using System;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay.Bridge
{
    /// <summary>
    /// Basic NODE and DEVICE test.
    /// </summary>
    public class Example02: BridgeService
    {
        #region Service
        
        public Example02(
            IHostApplicationLifetime appLifetime,
            SparkplugB.ClientServiceOptions spbOptions,
            ChannelReader<MTConnect.ClientServiceOutboundChannelFrame> mtcChannelReader,
            ChannelWriter<MTConnect.ClientServiceInboundChannelFrame> mtcChannelWriter,
            ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame> spbChannelReader,
            ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame> spbChannelWriter,
            ChannelWriter<bool> tsChannelWriter)
                : base(appLifetime, mtcChannelReader, mtcChannelWriter, spbChannelReader, spbChannelWriter, tsChannelWriter)
        {
            _spbOptions = spbOptions;
        }

        #endregion
        
        #region SparkplugB
        
        private SparkplugB.ClientServiceOptions _spbOptions;

        protected virtual async Task UpdateSpbDevicesBirth(MTConnectSharp.IIMTConnectClient client)
        {
            foreach (var device in client.Devices.Where(d => !d.IsAgent))
            {
                if (ResolveSparkplugBDeviceBirthCondition(client, device))
                {
                    await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                    {
                        Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_BIRTH,
                        Payload = new
                        {
                            deviceId = ResolveSparkplugBDeviceOptions(client, device),
                            mapper = DefineSparkplugBMetricMapper(),
                            data = WalkMTConnectDevice(device)
                        }
                    });
                }
            }
        }

        protected virtual async Task UpdateSpbDevicesDeath(MTConnectSharp.IIMTConnectClient client)
        {
            foreach (var device in client.Devices.Where(d => !d.IsAgent))
            {
                if (ResolveSparkplugBDeviceDeathCondition(client, device))
                {
                    await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                    {
                        Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DEATH,
                        Payload = new
                        {
                            deviceId = ResolveSparkplugBDeviceOptions(client, device)
                        }
                    });
                }
            }
        }
        
        protected virtual async Task UpdateSpbNodeBirth(MTConnectSharp.IIMTConnectClient client)
        {
            var nodeOptions = ResolveSparkplugBNodeOptions(client);
                
            await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
            {
                Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_BIRTH,
                Payload = new
                {
                    options = _spbOptions,
                    groupId = nodeOptions.Item1,
                    nodeId = nodeOptions.Item2,
                    mapper = DefineSparkplugBMetricMapper(),
                    data = WalkMTConnectDevice(client.GetAgent())
                }
            });
        }
        
        protected virtual async Task UpdateSpbNodeDeath(MTConnectSharp.IIMTConnectClient client)
        {
            await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
            {
                Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DEATH,
                Payload = new {  }
            });
        }

        protected virtual async Task UpdateSpbNodeData(MTConnectSharp.IIMTConnectClient client)
        {
            var avail = client.GetAgent().IsEventAvailable("AVAILABILITY");
            
            // TODO: also handle birth/death
            
            await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
            {
                Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DATA,
                Payload = new
                {
                    mapper = DefineSparkplugBMetricMapper(),
                    data = WalkMTConnectDevice(client.GetAgent())
                }
            });
        }
        
        protected virtual async Task UpdateSpbDevicesData(MTConnectSharp.IIMTConnectClient client)
        {
            foreach (var device in client.Devices.Where(d => !d.IsAgent))
            {
                var deviceAvail = device.IsEventAvailable("AVAILABILITY");
                
                // TODO: also handle birth/death
                
                await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                {
                    Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DATA,
                    Payload = new
                    {
                        deviceId = ResolveSparkplugBDeviceOptions(client, device),
                        mapper = DefineSparkplugBMetricMapper(),
                        data = WalkMTConnectDevice(device)
                    }
                });
            }
        }
        
        #endregion
        
        #region MTConnect
        
        protected override async Task OnMTConnectCurrentCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            if (ResolveSparkplugBNodeBirthCondition(client))
            {
                await UpdateSpbNodeBirth(client);

                await UpdateSpbDevicesBirth(client);
            }
            
            if (ResolveSparkplugBNodeDeathCondition(client))
            {
                await UpdateSpbDevicesDeath(client);

                await UpdateSpbNodeDeath(client);
            }
        }

        protected override async Task OnMTConnectCurrentFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            // call to /current happens only once during startup.  spb is not initialized yet.  nothing to do.
            // mtc client will continue retrying /current call

            return;
        }

        protected override async Task OnMTConnectSampleCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            // TODO: if we issue spb DEATH certs in SampleFailed, then we need to issue spb BIRTH certs here.
            return;
        }

        protected override async Task OnMTConnectSampleFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            // call to /sample occurs at defined intervals.
            // TODO: should we issue spb DEATH certs here?

            return;
        }

        protected override async Task OnMTConnectDataChanged(MTConnectSharp.IIMTConnectClient client, XDocument xml, MTConnectSharp.MTConnectClient.SamplePollResult poll)
        {
            var slimClient = CreateMTConnectSlimClient(client, poll);

            await UpdateSpbNodeData(slimClient);

            await UpdateSpbDevicesData(slimClient);
        }
        
        #endregion
    }
}