using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client.Options;
using Serilog;
using SparkplugNet.Core;
using SparkplugNet.Core.Application;
using SparkplugNet.VersionB;
using SparkplugNet.VersionB.Data;

namespace spb_app
{
    public class SpbApplicationService
    {
        private readonly List<Metric> VersionBMetrics = new ()
        {
            new Metric
            {
                Name = "Test", DataType = (uint)DataType.Double, DoubleValue = 1.20
            },
            new Metric
            {
                Name = "Test2",
                DataType = (uint)DataType.Boolean, BooleanValue = true
            }
        };

        private SparkplugApplication _application;
        
        public SpbApplicationService()
        {
            
        }

        public async Task Start()
        {
            var applicationMetrics = new List<Metric>(VersionBMetrics);
            _application = new SparkplugApplication(applicationMetrics, Log.Logger);
            var applicationOptions = new SparkplugApplicationOptions(
                "10.20.30.112:9001/mqtt",
                0,
                "application1",
                "",
                "",
                false,
                "scada1",
                TimeSpan.FromSeconds(30),
                true,
                new MqttClientOptionsBuilderWebSocketParameters(),
                null,
                new CancellationToken());

            await _application.Start(applicationOptions);
        }

        public async Task Stop()
        {
            await _application.Stop();
        }

        public ConcurrentDictionary<string, MetricState<Metric>> GetNodeStates()
        {
            if(_application!=null)
                return _application.NodeStates;

            return new ConcurrentDictionary<string, MetricState<Metric>>();
        }
        
        public ConcurrentDictionary<string, MetricState<Metric>> GetDeviceStates()
        {
            if(_application!=null)
                return _application.DeviceStates;

            return new ConcurrentDictionary<string, MetricState<Metric>>();
        }

        public List<Metric> GetKnownMetrics()
        {
            if(_application!=null)
                return _application.KnownMetrics;

            return new List<Metric>();
        }
    }
}