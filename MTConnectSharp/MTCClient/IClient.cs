using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MTConnectSharp
{
   public interface IMTConnectClient
	{
		string AgentUri { get; }
		string Sender { get; }
		Task<bool> GetProbe();
		Task StartStreaming();
		void StopStreaming();
		Task<bool> GetCurrent();
		Task<bool> GetSample();
		void SuppressDataItemChangeOnCurrent(bool suppress);
		ReadOnlyObservableCollection<IDevice> Devices { get; }
		TimeSpan UpdateInterval { get; set; }
	}
   
   public interface IIMTConnectClient
   {
	   string AgentUri { get; }
	   string Sender { get; }
	   ReadOnlyObservableCollection<IDevice> Devices { get; }
	   IDevice GetAgent();
   }
}
