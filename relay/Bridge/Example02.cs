using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;

namespace mtc_spb_relay.Bridge
{
    /// <summary>
    /// SpB NBIRTH, NDEATH
    /// </summary>
    public class Example02: BridgeService
    {
        private SparkplugB.ClientServiceOptions _spbOptions;
        
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

        protected override Func<dynamic, SparkplugNet.VersionB.Data.Metric> DefineSpbMetricMapper()
        {
            return base.DefineSpbMetricMapper();
        }

        protected override void ResolveMtConnectDataItem(
            List<dynamic> list, 
            string path, 
            MTConnectSharp.IDevice device, 
            MTConnectSharp.Component component,
            ReadOnlyObservableCollection<MTConnectSharp.DataItem> dataItems, 
            MTConnectSharp.DataItem dataItem)
        {
            base.ResolveMtConnectDataItem(list, path, device, component, dataItems, dataItem);
        }

        protected override void ResolveMtConnectComponent(
            List<dynamic> list, 
            string path, 
            MTConnectSharp.IDevice device, 
            ReadOnlyObservableCollection<MTConnectSharp.Component> components,
            MTConnectSharp.Component component)
        {
            base.ResolveMtConnectComponent(list, path, device, components, component);
        }

        protected override string ResolveMTConnectPath(string path, MTConnectSharp.Component component)
        {
            return base.ResolveMTConnectPath(path, component);
        }

        async Task updateNode(MTConnectSharp.IIMTConnectClient client)
        {
            if (!client.GetAgent().IsEventAvailable("AVAILABILITY"))
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
            }
            else
            {
                await SendToSpB(new SparkplugB.ClientServiceInboundChannelFrame()
                {
                    Type = SparkplugB.ClientServiceInboundChannelFrame.FrameTypeEnum.NODE_DEATH,
                    Payload = new {  }
                });
            }
        }

        protected override void OnServiceStart()
        {
            
        }

        protected override void OnServiceStop()
        {
            base.OnServiceStop();
        }

        protected override Task OnMTConnectServiceError(Exception ex)
        {
            return base.OnMTConnectServiceError(ex);
        }

        protected override async Task OnMTConnectProbeCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            
        }

        protected override Task OnMTConnectProbeFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            return base.OnMTConnectProbeFailed(client, ex);
        }

        protected override async Task OnMTConnectCurrentCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            await updateNode(client);
        }

        protected override Task OnMTConnectCurrentFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            return base.OnMTConnectCurrentFailed(client, ex);
        }

        protected override async Task OnMTConnectSampleCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            await updateNode(client);
        }

        protected override Task OnMTConnectSampleFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            return base.OnMTConnectSampleFailed(client, ex);
        }

        protected override Task OnMTConnectDataChanged(MTConnectSharp.IIMTConnectClient client, XDocument xml, MTConnectSharp.MTConnectClient.SamplePollResult poll)
        {
            return base.OnMTConnectDataChanged(client, xml, poll);
        }
    }
}