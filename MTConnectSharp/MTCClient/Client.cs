using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;

namespace MTConnectSharp
{
   /// <summary>
   /// Connects to a single agent and streams data from it.
   /// </summary>
   public class MTConnectClient : IMTConnectClient, IIMTConnectClient, IDisposable
   {
		public class SamplePollResult
		{
			public bool HasUpdates
			{
				get
				{
					return StartingSequence != EndingSequence;
				}
			}
			public long StartingSequence { get; set; }
			public long EndingSequence { get; set; }
			public Dictionary<string,DataItem> DataItems { get; set; }
			public SamplePollResult()
			{
			   DataItems = new Dictionary<string, DataItem>();
			}
		}
	   
		public Func<IMTConnectClient, XDocument, Task> OnProbeCompleted = async (a, b) => {  };

		public Func<IMTConnectClient, Exception, Task> OnProbeFailed = async (a, b) => {  };

		public Func<IMTConnectClient, XDocument, Task> OnCurrentCompleted = async (a, b) => {  };

		public Func<IMTConnectClient, Exception, Task> OnCurrentFailed = async (a, b) => {  };

		public Func<IMTConnectClient, XDocument, Task> OnSampleCompleted = async (a, b) => {  };

		public Func<IMTConnectClient, Exception, Task> OnSampleFailed = async (a, b) => {  };
		
		public Func<IMTConnectClient, XDocument, SamplePollResult, Task> OnDataChanged = async (a, b, c) => {  };
		
		/// <summary>
		/// The base uri of the agent
		/// </summary>
		public string AgentUri { get; set; }

		/// <summary>
		/// Time in milliseconds between sample queries when simulating a streaming connection
		/// </summary>
		public TimeSpan UpdateInterval { get; set; }

		public string Sender { get; private set; }
		
		/// <summary>
		/// Devices on the connected agent
		/// </summary>
		public ReadOnlyObservableCollection<Device> Devices
		{
			get;
			private set;
		}
		private ObservableCollection<Device> _devices;

		/// <summary>
		/// Dictionary Reference to all data items by id for better performance when streaming
		/// </summary>
		private Dictionary<string, DataItem> _dataItemsDictionary = new Dictionary<string,DataItem>(); 
		
		/// <summary>
		/// RestSharp RestClient
		/// </summary>
		private RestClient _restClient;
		
		/// <summary>
		/// Not actually parsing multipart stream - this timer fires sample queries to simulate streaming
		/// </summary>
		private Timer _streamingTimer;

		/// <summary>
		/// Last sequence number read from current or sample
		/// </summary>
		private long _lastSequence;
		
		public long LastSequence
		{
			get => _lastSequence; 
		}

		private bool _probeStarted = false;
		private bool _probeCompleted = false;
		
		private bool _currentStarted = false;
		private bool _currentCompleted = false;
		
		private bool _sampleStarted = false;
		private bool _sampleCompleted = false;

		private bool _suppressDataItemChangeOnCurrent { get; set; }
		public void SuppressDataItemChangeOnCurrent(bool suppress)
		{
			_suppressDataItemChangeOnCurrent = suppress;
		}
		
		/// <summary>
		/// Initializes a new Client 
		/// </summary>
		public MTConnectClient()
		{
			UpdateInterval = TimeSpan.FromMilliseconds(2000);

			_devices = new ObservableCollection<Device>();
			Devices = new ReadOnlyObservableCollection<Device>(_devices);
		}

		public IDevice GetAgent()
		{
			return Devices.Single(d => d.IsAgent);
		}

		/// <summary>
		/// Starts sample polling and updating DataItem values as they change
		/// </summary>
		public async Task StartStreaming()
		{
			if (_streamingTimer?.Enabled == true)
			{
				return;
			}

			await GetCurrent();

			_streamingTimer = new Timer(UpdateInterval.TotalMilliseconds);
			_streamingTimer.Elapsed += StreamingTimerElapsed;
			_streamingTimer.Start();
		}

		/// <summary>
		/// Stops sample polling
		/// </summary>
		public void StopStreaming()
		{
			_streamingTimer.Stop();
		}

		/// <summary>
		/// Gets current response and updates DataItems
		/// </summary>
		public async Task<bool> GetCurrent()
		{
			if (!_probeCompleted)
			{
				throw new InvalidOperationException("Cannot get DataItem values. Agent has not been probed yet.");
			}

			if (_currentStarted && !_currentCompleted)
			{
				throw new InvalidOperationException("Cannot start a new Current when one is still running.");
			}
			
			var request = new RestRequest
			{
				Resource = "current"
			};

			try
			{
				_currentStarted = true;
				
				request.AddHeader("Accept", "application/xml");
				var result = await _restClient.ExecuteGetAsync(request);
				
				var parsed = ParseStream(result);
			
				await OnCurrentCompleted(this, parsed.Item1);
				
				if
				(
					(parsed.Item2.HasUpdates && !_currentStarted) || 
					(parsed.Item2.HasUpdates && _currentStarted && !_suppressDataItemChangeOnCurrent)
				)
					await OnDataChanged(this, parsed.Item1, parsed.Item2);

				_currentCompleted = true;
				_currentStarted = false;
				
				return true;
			}
			catch (Exception ex)
			{
				_currentStarted = false;

				await OnCurrentFailed(this, ex);
				
				return false;
			} 
			
		}

		/// <summary>
		/// Gets probe response from the agent and populates the devices collection
		/// </summary>
		public async Task<bool> GetProbe()
		{
			if (_probeStarted && !_probeCompleted)
			{
				throw new InvalidOperationException("Cannot start a new GetProbe when one is still running.");
			}

			_restClient = new RestClient
			{
				BaseUrl = new Uri(AgentUri)
			};

			var request = new RestRequest
			{
				Resource = "probe"
			};

			try
			{
				_probeStarted = true;
				
				request.AddHeader("Accept", "application/xml");
				var result = await _restClient.ExecuteGetAsync(request); 
				
				var xdoc = ParseProbeResponse(result);
				
				_probeCompleted = true;
				_probeStarted = false;
				
				await OnProbeCompleted(this, xdoc);

				return true;
			}
			catch (Exception ex)
			{
				_probeStarted = false;
				
				await OnProbeFailed(this, ex);

				return false;
			} 
		}
		
		/// <summary>
		/// Parses IRestResponse from a probe command into a Device collection
		/// </summary>
		/// <param name="response">An IRestResponse from a probe command</param>
		private XDocument ParseProbeResponse(IRestResponse response)
		{
			var xdoc = XDocument.Load(new StringReader(response.Content));

			Sender = xdoc
				.Descendants()
				.Single(d => d.Name.LocalName == "Header")
				.Attributes()
				.Single(a => a.Name.LocalName == "sender").Value;
			
			if (_devices.Any())
				_devices.Clear();

			var devices = xdoc.Descendants()
				.Where(d => d.Name.LocalName == "Devices")
				.Take(1) // needed? 
				.SelectMany(d => d.Elements())
				.Select(d => new Device(d));
			
			_devices.AddRange(devices);

			BuildDataItemDictionary();

			return xdoc;
		}

		/// <summary>
		/// Loads DataItemRefList with all data items from all devices
		/// </summary>
		private void BuildDataItemDictionary()
		{
			_dataItemsDictionary = _devices.SelectMany(d =>
				d.DataItems.Concat(GetAllDataItems(d.Components))
			).ToDictionary(i => i.Id, i => i);
		}

		/// <summary>
		/// Recursive function to get DataItems list from a Component collection
		/// </summary>
		/// <param name="components">Collection of Components</param>
		/// <returns>Collection of DataItems from passed Component collection</returns>
		private static List<DataItem> GetAllDataItems(IReadOnlyList<Component> components)
		{
			var queue = new Queue<Component>(components);
			var dataItems = new List<DataItem>();
			while(queue.Count > 0)
			{
				var component = queue.Dequeue();
				foreach (var c in component.Components)
				   queue.Enqueue(c);
				dataItems.AddRange(component.DataItems);
			}
			
			return dataItems;
		}

		public async Task<bool> GetSample()
		{
			var request = new RestRequest
			{
				Resource = "sample"
			};
			
			try
			{
				_sampleStarted = true;
				
				request.AddParameter("from", _lastSequence + 1);
				request.AddHeader("Accept", "application/xml");
				var result = await _restClient.ExecuteGetAsync(request);
				
				var parsed = ParseStream(result);

				_sampleCompleted = true;
				_sampleStarted = false;
				
				await OnSampleCompleted(this, parsed.Item1);
				
				if (parsed.Item2.HasUpdates)
					await OnDataChanged(this, parsed.Item1, parsed.Item2);

				return true;
			}
			catch (Exception ex)
			{
				_sampleStarted = false;

				await OnSampleFailed(this, ex);

				return false;
			} 
		}
		
		private async void StreamingTimerElapsed(object sender, ElapsedEventArgs e)
		{
			await GetSample();
		}

		/// <summary>
		/// Parses response from a current or sample request, updates changed data items and fires events
		/// </summary>
		/// <param name="response">IRestResponse from the MTConnect request</param>
		private (XDocument, SamplePollResult) ParseStream(IRestResponse response)
		{
			XDocument xdoc = null;
			SamplePollResult pollResult = new SamplePollResult();
			
			using (StringReader sr = new StringReader(response.Content))
			{
				xdoc = XDocument.Load(sr);

				var currentSequence = Convert
					.ToInt64(xdoc.Descendants().First(e => e.Name.LocalName == "Header")
					.Attribute("lastSequence").Value);

				pollResult.StartingSequence = _lastSequence;
				pollResult.EndingSequence = currentSequence;
				
				_lastSequence = currentSequence;

				var xmlDataItems = xdoc.Descendants()
					.Where(e => e.Attributes().Any(a => a.Name.LocalName == "dataItemId"));
	            
				if (xmlDataItems.Any())
				{
					var dataItems = xmlDataItems.Select(e => new {
						id = e.Attribute("dataItemId").Value,
						timestamp = DateTime.Parse(e.Attribute("timestamp").Value, null, 
							System.Globalization.DateTimeStyles.RoundtripKind),
						value = e.Value,
						sequence = Convert.ToInt64(e.Attribute("sequence").Value)
					})
	               .OrderBy(i => i.sequence)
	               .ToList();

	               foreach (var item in dataItems)
	               {
	                  var dataItem = _dataItemsDictionary[item.id];
	                  var sample = new DataItemSample(item.value.ToString(), item.timestamp, item.sequence);
	                  dataItem.AddSample(sample);
	                  
	                  if(!pollResult.DataItems.ContainsKey(dataItem.Id))
		                  pollResult.DataItems.Add(dataItem.Id, dataItem);
	               }
				}
			}

			return (xdoc, pollResult);
		}

		public void Dispose()
		{
			_streamingTimer?.Dispose();
		}
	}
}
