using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay.Bridge
{
    /// <summary>
    /// SpB NBIRTH, NDATA, NDEATH
    /// </summary>
    public class Example02: BridgeService
    {
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

        private SparkplugB.ClientServiceOptions _spbOptions;
        
        async Task updateSpbNode(MTConnectSharp.IIMTConnectClient client)
        {
            var agentAvail = client.GetAgent().IsEventAvailable("AVAILABILITY");
            
            if (agentAvail.Item1 != null && !agentAvail.Item2)
            {
                await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                {
                    Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_BIRTH,
                    Payload = new
                    {
                        options = _spbOptions,
                        groupId = client.Sender,
                        nodeId = client.GetAgent().UUID,
                        mapper = DefineSpbMetricMapper(),
                        data = WalkMTConnectDevice(client.GetAgent())
                    }
                });

                foreach (var device in client.Devices.Where(d => !d.IsAgent))
                {
                    var deviceAvail = device.IsEventAvailable("AVAILABILITY");

                    if (deviceAvail.Item1 != null && deviceAvail.Item2)
                    {
                        await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                        {
                            Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_BIRTH,
                            Payload = new
                            {
                                deviceId = device.UUID,
                                mapper = DefineSpbMetricMapper(),
                                data = WalkMTConnectDevice(device)
                            }
                        });
                    }
                }
            }
            else
            {
                foreach (var device in client.Devices.Where(d => !d.IsAgent))
                {
                    var deviceAvail = device.IsEventAvailable("AVAILABILITY");

                    if (deviceAvail.Item1 != null && deviceAvail.Item2)
                    {
                        await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                        {
                            Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DEATH,
                            Payload = new
                            {
                                deviceId = device.UUID
                            }
                        });
                    }
                }
                
                await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                {
                    Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DEATH,
                    Payload = new {  }
                });
            }
        }

        async Task updateSpbNode(MTConnectSharp.IIMTConnectClient client, MTConnectSharp.MTConnectClient.SamplePollResult poll)
        {
            var slimClient = CreateMTConnectSlimClient(client, poll);

            var avail = slimClient.GetAgent().IsEventAvailable("AVAILABILITY");
            
            var frame = new SparkplugB.ClientServiceInboundChannelFrame()
            {
                Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DATA,
                Payload = new
                {
                    options = _spbOptions,
                    groupId = client.Sender,
                    nodeId = slimClient.GetAgent().UUID,
                    mapper = DefineSpbMetricMapper(),
                    data = WalkMTConnectDevice(slimClient.GetAgent())
                }
            };

            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(frame.Payload.data));
            await SendToSpB(frame);
            
            foreach (var device in slimClient.Devices.Where(d => !d.IsAgent))
            {
                var deviceAvail = device.IsEventAvailable("AVAILABILITY");
                
                //TODO also handle birth/death
                
                await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                {
                    Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.DEVICE_DATA,
                    Payload = new
                    {
                        deviceId = device.UUID,
                        mapper = DefineSpbMetricMapper(),
                        data = WalkMTConnectDevice(device)
                    }
                });
            }
        }
        
        protected override async Task OnMTConnectCurrentCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            await updateSpbNode(client);
        }

        protected override async Task OnMTConnectSampleCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            
        }

        protected override async Task OnMTConnectDataChanged(MTConnectSharp.IIMTConnectClient client, XDocument xml, MTConnectSharp.MTConnectClient.SamplePollResult poll)
        {
            await updateSpbNode(client, poll);
        }
    }
}