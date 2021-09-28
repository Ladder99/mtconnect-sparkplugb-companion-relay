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
            // returns spb group and node_id
            return (_mtcVersion, client.GetAgent().Id);
        }
        
        protected override string ResolveSparkplugBDeviceOptions(
            MTConnectSharp.IIMTConnectClient client,
            MTConnectSharp.IDevice device)
        {
            // returns device_id
            return device.UUID;
        }
        
        // TODO: mazak demo agent avail is always unavailable
        // -- 09/28 not anymore
        protected override bool ResolveSparkplugBNodeBirthCondition(
            MTConnectSharp.IIMTConnectClient client)
        {
            // is it time to publish node birth?
            var avail = client.GetAgent().IsEventAvailable("AVAILABILITY");
            return avail.Item1 != null && avail.Item2 == true;
        }

        protected override string ResolveMTConnectPath(
            string path, 
            MTConnectSharp.IComponent component)
        {
            // create path to mtc component when walking mtc component tree
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
            // list of mtc data items to be transformed into spb metrics
            var properties = new List<(string, string)>();
            
            properties.Add(("nativeUnits", dataItem.NativeUnits));
            properties.Add(("Units", dataItem.Units));
            properties.Add(("cat", "meow"));
            
            list.Add(new
            {
                name = $"{path}/{dataItem.Type}-{dataItem.Id}",
                value = dataItem.CurrentSample.Value,
                properties
            });
        }

        protected override Func<dynamic, SparkplugNet.VersionB.Data.Metric> DefineSparkplugBMetricMapper()
        {
            // func to convert mtc data to spb metrics
            Func<dynamic, SparkplugNet.VersionB.Data.Metric> func = o =>
            {
                var ps = new SparkplugNet.VersionB.Data.PropertySet();

                foreach (var property in o.properties)
                {
                    ps.Keys.Add(property.Item1);

                    var pv = new SparkplugNet.VersionB.Data.PropertyValue();
                    pv.Type = (uint)SparkplugNet.VersionB.Data.DataType.String;
                    pv.StringValue = property.Item2;
                    ps.Values.Add(pv);
                }
                
                var metric = new SparkplugNet.VersionB.Data.Metric()
                {
                    Name = o.name,
                    DataType = (uint)SparkplugNet.VersionB.Data.DataType.String,
                    StringValue = o.value,
                    Properties = ps
                };
                
                return metric;
            };

            return func;
        }

        protected override async Task OnMTConnectProbeCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            // get mtc version from probe header
            _mtcVersion = xml
                .Descendants()
                .Single(d => d.Name.LocalName == "Header")
                .Attributes()
                .Single(a => a.Name.LocalName == "version").Value;
        }
    }
}