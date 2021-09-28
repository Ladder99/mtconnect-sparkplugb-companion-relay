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
    /// https://drive.google.com/file/d/1PAJKLUUCviN_Q3dus65t5fl_yBNrjSxp/view?usp=sharing
    /// </summary>
    public class AaronV3: Example02
    {
        #region Service
        
        public AaronV3(
            IHostApplicationLifetime appLifetime,
            SparkplugB.ClientServiceOptions spbOptions,
            ChannelReader<MTConnect.ClientServiceOutboundChannelFrame> mtcChannelReader,
            ChannelWriter<MTConnect.ClientServiceInboundChannelFrame> mtcChannelWriter,
            ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame> spbChannelReader,
            ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame> spbChannelWriter,
            ChannelWriter<bool> tsChannelWriter)
                : base(appLifetime, spbOptions, mtcChannelReader, mtcChannelWriter, spbChannelReader, spbChannelWriter, tsChannelWriter)
        {
           
        }

        #endregion

        private string _mtcVersion = "0.0.0.0";
        
        protected override (string, string) ResolveSparkplugBNodeOptions(MTConnectSharp.IIMTConnectClient client)
        {
            return (_mtcVersion, client.GetAgent().Id);
        }
        
        protected override string ResolveSparkplugBDeviceOptions(
            MTConnectSharp.IIMTConnectClient client,
            MTConnectSharp.IDevice device)
        {
            return device.UUID;
        }

        protected override string ResolveMTConnectPath(
            string path, 
            MTConnectSharp.IComponent component)
        {
            return $"{path}/{component.Type}-{component.Id}";
        }
        
        protected override void ResolveMtConnectDataItem(
            List<dynamic> list, 
            string path, 
            MTConnectSharp.IDevice device,
            MTConnectSharp.IComponent component,
            ReadOnlyObservableCollection<MTConnectSharp.IDataItem> dataItems,
            MTConnectSharp.IDataItem dataItem)
        {
            list.Add(new
            {
                name = $"{path}/{dataItem.Type}-{dataItem.Id}",
                value = dataItem.CurrentSample.Value
            });
        }
        
        // TODO: mazak demo agent avail is always unavailable
        protected override bool ResolveSparkplugBNodeBirthCondition(
            MTConnectSharp.IIMTConnectClient client)
        {
            var avail = client.GetAgent().IsEventAvailable("AVAILABILITY");
            return avail.Item1 != null && avail.Item2 == false;
        }
        
        protected override async Task OnMTConnectProbeCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            _mtcVersion = xml
                .Descendants()
                .Single(d => d.Name.LocalName == "Header")
                .Attributes()
                .Single(a => a.Name.LocalName == "version").Value;
        }
    }
}