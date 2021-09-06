using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Loader;
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
      
      public void Run()
      {
         var client = new MTConnectSharp.MTConnectClient()
         {
            AgentUri = this.AgentUri,
            UpdateInterval = TimeSpan.FromSeconds(this.UpdateInterval)
         };

         bool currentCompleted = false;
         
         client.ProbeCompleted += (sender, info) => {
            var items = client.Devices
               .SelectMany(d => d.DataItems.Select(i => new { d = d.LongName, i = i.LongName }))
               .ToArray();

            Console.WriteLine($"Number of DataItems: {items.Count()}");

            client.StartStreaming();
         };

         client.GetCurrentCompleted += (sender, info) =>
         {
            currentCompleted = true;
         };
         
         client.GetSampleCompleted += (sender, info) =>
         {

         };
         
         client.DataItemChanged += (sender, info) =>
         {
            if (!currentCompleted)
               return;
            
            Console.WriteLine($"sequence: {((MTConnectClient.DataChangedEventArgs)info).StartingSequence} -> {((MTConnectClient.DataChangedEventArgs)info).EndingSequence}");
            
            foreach (var kv in ((MTConnectClient.DataChangedEventArgs)info).DataItems)
            {
               Console.WriteLine($"{kv.Key} : {kv.Value.PreviousSample?.Value} => {kv.Value.CurrentSample.Value}");
            }
            
            Console.WriteLine("");
         };

         client.Probe();
      }
   }
}