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
    class Program
    {
        private static readonly CancellationTokenSource CancellationTokenSource = new ();

        private static readonly List<VersionBData.Metric> VersionBMetrics = new ()
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
        
        public static async Task Main()
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();

                await RunVersionB();

                Log.Information("Simulation is done.");
            }
            catch (Exception ex)
            {
                Log.Error("An exception occurred: {Exception}", ex);
            }
            finally
            {
                Console.ReadKey();
            }
        }
        
        private static async Task RunVersionB()
        {
            await RunVersionBNode();
        }
        
        private static async Task RunVersionBNode()
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
        
        private static void OnVersionBApplicationDisconnected()
        {
            // Do something.
        }
        
        private static void OnVersionBNodeDisconnected()
        {
            // Do something.
        }
        
        private static void OnVersionBNodeNodeCommandReceived(VersionBData.Metric metric)
        {
            // Do something.
        }
        
        private static void OnVersionBNodeStatusMessageReceived(string status)
        {
            // Do something.
        }
        
        private static void OnVersionBNodeDeviceBirthReceived(KeyValuePair<string, List<VersionBData.Metric>> deviceData)
        {
            // Do something.
        }
        
        private static void OnVersionBNodeDeviceCommandReceived(VersionBData.Metric metric)
        {
            // Do something.
        }
        
        private static void OnVersionBNodeDeviceDeathReceived(string deviceIdentifier)
        {
            // Do something.
        }
    }
}