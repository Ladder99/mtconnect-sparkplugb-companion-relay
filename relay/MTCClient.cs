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

         client.OnProbeCompleted = async (client, xml) => 
         {
            Console.WriteLine("OK: /probe");
            
            var items = client.Devices
               .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
               .ToArray();

            // control OnDataChanged during call to current
            client.SuppressDataItemChangeOnCurrent(true);   
            
            await client.StartStreaming();
         };

         client.OnProbeFailed = async (client, ex) =>
         {
            Console.WriteLine("ERR: /probe");
            Console.WriteLine(ex);
         };
         
         client.OnCurrentCompleted = async (client, xml) =>
         {
            Console.WriteLine("OK: /current");
         };
         
         client.OnCurrentFailed = async (client, ex) =>
         {
            Console.WriteLine("ERR: /current");
            Console.WriteLine(ex);
         };
         
         client.OnSampleCompleted = async (client, xml) =>
         {
            Console.WriteLine("OK: /sample");
         };
         
         client.OnDataChanged = async (client, xml, poll) =>
         {
            // dataitem sample changed
            
            Console.WriteLine($"sequence: {poll.StartingSequence} -> {poll.EndingSequence}");
            
            foreach (var kv in poll.DataItems)
            {
               Console.WriteLine($"{kv.Key} : (seq:{kv.Value.PreviousSample?.Sequence}){kv.Value.PreviousSample?.Value} => (seq:{kv.Value.CurrentSample?.Sequence}){kv.Value.CurrentSample.Value}");
            }
            
            Console.WriteLine("");
         };

         await client.Probe();
      }
   }
}