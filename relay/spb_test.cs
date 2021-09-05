using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

using SparkplugNet.VersionB;
using SparkplugNet.Core.Node;

using VersionBData = SparkplugNet.VersionB.Data;

namespace mtc_spb_relay
{
    public class spb_test
    {
        private readonly CancellationTokenSource CancellationTokenSource = new ();

        private readonly List<VersionBData.Metric> VersionBMetrics = new ()
        {
            new VersionBData.Metric
            {
                Name = "Test", DataType = (uint)VersionBData.DataType.Double, DoubleValue = 4.20
            },
            new VersionBData.Metric
            {
                Name = "Test2",
                DataType = (uint)VersionBData.DataType.Boolean, BooleanValue = true
            }
        };
        
        public async Task Run()
        {
            await RunVersionB();
        }
        
        private async Task RunVersionB()
        {
            await RunVersionBNode();
        }
        
        private async Task RunVersionBNode()
        {
            var nodeMetrics = new List<VersionBData.Metric>(VersionBMetrics);
            var node = new SparkplugNode(nodeMetrics, Log.Logger);
            //var nodeOptions = new SparkplugNodeOptions("localhost", 1883, "node 1", "user", "password", false, "scada1", "group1", "node1", TimeSpan.FromSeconds(30), null, null, CancellationTokenSource.Token);

            var nodeOptions = new SparkplugNodeOptions(
                "10.20.30.114", 
                1883, 
                "node 1", 
                "admin", 
                "password", 
                false, 
                "scada1", 
                "group1", 
                "node1", 
                TimeSpan.FromSeconds(30), 
                null, 
                null,
                CancellationTokenSource.Token);
            
            // Start a node.
            Log.Information("Starting node...");
            await node.Start(nodeOptions);
            Log.Information("Node started...");

            // Publish node metrics.
            await node.PublishMetrics(nodeMetrics);

            // Get the known node metrics from a node.
            // ReSharper disable once UnusedVariable
            var currentlyKnownMetrics = node.KnownMetrics;

            // Check whether a node is connected.
            // ReSharper disable once UnusedVariable
            var isApplicationConnected = node.IsConnected;

            // Handle the node's disconnected event.
            node.OnDisconnected += OnVersionBNodeDisconnected;

            // Handle the node's node command received event.
            node.NodeCommandReceived += OnVersionBNodeNodeCommandReceived;

            // Handles the node's status message received event.
            node.StatusMessageReceived += OnVersionBNodeStatusMessageReceived;

            // Get the known devices.
            // ReSharper disable once UnusedVariable
            var knownDevices = node.KnownDevices;

            // Handling devices.
            const string DeviceIdentifier = "device1";
            var deviceMetrics = new List<VersionBData.Metric>(VersionBMetrics);

            // Publish a device birth message.
            await node.PublishDeviceBirthMessage(deviceMetrics, DeviceIdentifier);

            // Publish a device data message.
            await node.PublishDeviceData(deviceMetrics, DeviceIdentifier);

            // Publish a device death message.
            await node.PublishDeviceDeathMessage(DeviceIdentifier);

            // Handle the node's device birth received event.
            node.DeviceBirthReceived += OnVersionBNodeDeviceBirthReceived;

            // Handle the node's device command received event.
            node.DeviceCommandReceived += OnVersionBNodeDeviceCommandReceived;

            // Handle the node's device death received event.
            node.DeviceDeathReceived += OnVersionBNodeDeviceDeathReceived;

            // Stopping a node.
            await node.Stop();
            Log.Information("Node stopped...");
        }
        
        private void OnVersionBApplicationDisconnected()
        {
            // Do something.
        }
        
        private void OnVersionBNodeDisconnected()
        {
            // Do something.
        }
        
        private void OnVersionBNodeNodeCommandReceived(VersionBData.Metric metric)
        {
            // Do something.
        }
        
        private void OnVersionBNodeStatusMessageReceived(string status)
        {
            // Do something.
        }
        
        private void OnVersionBNodeDeviceBirthReceived(KeyValuePair<string, List<VersionBData.Metric>> deviceData)
        {
            // Do something.
        }
        
        private void OnVersionBNodeDeviceCommandReceived(VersionBData.Metric metric)
        {
            // Do something.
        }
        
        private void OnVersionBNodeDeviceDeathReceived(string deviceIdentifier)
        {
            // Do something.
        }
    }
}