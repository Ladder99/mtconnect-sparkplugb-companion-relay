﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MTConnectSharp
{
   public interface IMTConnectClient
	{
		string AgentUri { get; set; }
		string Sender { get; }
		Task<bool> GetProbe();
		Task StartStreaming();
		void StopStreaming();
		Task<bool> GetCurrent();
		Task<bool> GetSample();
		void SuppressDataItemChangeOnCurrent(bool suppress);
		ReadOnlyObservableCollection<Device> Devices { get; }
		TimeSpan UpdateInterval { get; set; }
	}
   
   public interface IIMTConnectClient
   {
	   string AgentUri { get; set; }
	   string Sender { get; }
	   ReadOnlyObservableCollection<Device> Devices { get; }
	   IDevice GetAgent();
   }
}
