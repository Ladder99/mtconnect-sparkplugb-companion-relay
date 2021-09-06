using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MTConnectSharp
{
   public interface IMTConnectClient
	{
		string AgentUri { get; set; }
		Task Probe();
		Task StartStreaming();
		void StopStreaming();
		Task GetCurrentState();
		void SuppressDataItemChangeOnCurrent(bool suppress);
		ReadOnlyObservableCollection<Device> Devices { get; }
		TimeSpan UpdateInterval { get; set; }
	}
}
