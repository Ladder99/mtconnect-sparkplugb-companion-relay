using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using Newtonsoft.Json.Linq;

namespace mtc_spb_relay.Bridge
{
    /// <summary>
    /// Connect to generic MQTT server and publish components and data items when MTC probe completes.
    /// </summary>
    public class Example01: BridgeService
    {
        
        private string _broker_ip = "192.168.2.103";
        private int _broker_port = 1883;
        private bool _broker_anonymous = true;
        private string _broker_user = "";
        private string _broker_pass = "";
        
        private IMqttClientOptions _options;
        private IMqttClient _client;
        
        public Example01(
            IHostApplicationLifetime appLifetime,
            ChannelReader<MTConnect.ClientServiceOutboundChannelFrame> mtcChannelReader,
            ChannelWriter<MTConnect.ClientServiceInboundChannelFrame> mtcChannelWriter,
            ChannelReader<SparkplugB.ClientServiceOutboundChannelFrame> spbChannelReader,
            ChannelWriter<SparkplugB.ClientServiceInboundChannelFrame> spbChannelWriter,
            ChannelWriter<bool> tsChannelWriter)
                : base(appLifetime, mtcChannelReader, mtcChannelWriter, spbChannelReader, spbChannelWriter, tsChannelWriter)
        {
            
        }

        protected override void OnServiceStart()
        {
            var factory = new MqttFactory();

            var builder = new MqttClientOptionsBuilder()
                .WithTcpServer(_broker_ip, _broker_port);
            
            if (!_broker_anonymous)
            {
                var creds = new MqttClientCredentials();
                byte[] passwordBuffer = null;

                if (!string.IsNullOrEmpty(_broker_pass))
                    passwordBuffer = Encoding.UTF8.GetBytes(_broker_pass);
                
                creds.Username = _broker_user;
                creds.Password = passwordBuffer;
                builder.WithCredentials(creds);
            }

            _options = builder.Build();
            
            _client = factory.CreateMqttClient();
        }

        protected override void OnServiceStop()
        {
            base.OnServiceStop();
        }

        private async Task PublishAsync(string topic, string payload, bool retained = false)
        {
            if (_client.IsConnected)
            {
                var msg = new MqttApplicationMessageBuilder()
                    .WithRetainFlag(retained)
                    .WithTopic(topic)
                    .WithPayload(payload)
                    .Build();
                
                await _client.PublishAsync(msg, CancellationToken.None);
            }
        }
        
        protected override Task OnMTConnectServiceError(Exception ex)
        {
            return base.OnMTConnectServiceError(ex);
        }

        protected override async Task OnMTConnectProbeCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            await _client.ConnectAsync(_options, CancellationToken.None);
            
            foreach (var device in client.Devices)
            {
                await PublishComponent($"mtc/{device.Name}", device.Components);
                await PublishDataItems($"mtc/{device.Name}", device.DataItems);
            }
        }

        async Task PublishComponent(string path, ReadOnlyObservableCollection<MTConnectSharp.IComponent> components)
        {
            foreach (var component in components)
            {
                await PublishAsync($"{path}/{component.Id}", JObject.FromObject(new
                {
                    component.Id,
                    component.Name,
                    xml = component.Model.ToString()
                }).ToString());

                await PublishDataItems($"{path}/{component.Id}", component.DataItems);
                
                await PublishComponent($"{path}/{component.Id}", component.Components);
            }
        }

        async Task PublishDataItems(string path, ReadOnlyObservableCollection<MTConnectSharp.IDataItem> dataitems)
        {
            foreach (var dataitem in dataitems)
            {
                await PublishAsync($"{path}/{dataitem.Id}", JObject.FromObject(new
                {
                    dataitem.Id,
                    dataitem.Name,
                    dataitem.Units,
                    dataitem.NativeUnits,
                    dataitem.Category,
                    dataitem.Type,
                    dataitem.SubType,
                    xml = dataitem.Model.ToString()
                }).ToString());
            }
        }
        
        protected override Task OnMTConnectProbeFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            return base.OnMTConnectProbeFailed(client, ex);
        }

        protected override async Task OnMTConnectCurrentCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            
        }

        protected override Task OnMTConnectCurrentFailed(MTConnectSharp.IIMTConnectClient client, Exception ex)
        {
            return base.OnMTConnectCurrentFailed(client, ex);
        }

        protected override Task OnMTConnectSampleCompleted(MTConnectSharp.IIMTConnectClient client, XDocument xml)
        {
            return base.OnMTConnectSampleCompleted(client, xml);
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