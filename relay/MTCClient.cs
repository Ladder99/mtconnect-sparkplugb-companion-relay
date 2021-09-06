using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Loader;
using System.Threading.Tasks;
using MTConnectSharp;

namespace mtc_spb_relay
{
   public class MTCClient
   {
      public string AgentUri { get; private set; }
      public int UpdateInterval { get; private set; }
      
      public MTCClient(string agentUri = "http://mtconnect.mazakcorp.com:5717", int updateInterval = 1)
      {
         AgentUri = agentUri;
         UpdateInterval = updateInterval;
      }
      
      public async Task Run()
      {
         var client = new MTConnectSharp.MTConnectClient()
         {
            AgentUri = this.AgentUri,
            UpdateInterval = TimeSpan.FromSeconds(this.UpdateInterval)
         };

         client.ProbeCompleted += (sender, info) => {
            var items = client.Devices
               .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
               .ToArray();

            Console.WriteLine($"Number of DataItems: {items.Count()}");

            client.SuppressDataItemChangeOnCurrent(true);   // control DataItemChanged handler during call to current
            client.StartStreaming();
         };

         client.GetCurrentCompleted += (sender, info) =>
         {
            // probe and current completed
         };
         
         client.GetSampleCompleted += (sender, info) =>
         {
            // sample poll completed
         };
         
         client.DataItemChanged += (sender, info) =>
         {
            // dataitem sample changed
            
            Console.WriteLine($"sequence: {((MTConnectClient.DataChangedEventArgs)info).StartingSequence} -> {((MTConnectClient.DataChangedEventArgs)info).EndingSequence}");
            
            foreach (var kv in ((MTConnectClient.DataChangedEventArgs)info).DataItems)
            {
               Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
            }
            
            Console.WriteLine("");
         };

         await client.Probe();
      }
   }
}