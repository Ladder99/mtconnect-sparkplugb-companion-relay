using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MTConnectSharp
{
	/// <summary>
   /// Represents a device in the MTConnect probe response
   /// </summary>
   public class Device : MTCItemBase, IDevice
	{
		public string UUID { get; private set; }
		
		/// <summary>
		/// Description of the device
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Manufacturer of the device
		/// </summary>
		public string Manufacturer { get; private set; }

		/// <summary>
		/// Serial number of the device
		/// </summary>
		public string SerialNumber { get; private set; }
		
		public bool IsAgent { get; private set; }
		
		public XElement Model { get; private set; }

		/// <summary>
		/// The DataItems which are direct children of the device
		/// </summary>
		private ObservableCollection<IDataItem> _dataItems = new ObservableCollection<IDataItem>();

		/// <summary>
		/// The components which are direct children of the device
		/// </summary>
		private ObservableCollection<IComponent> _components = new ObservableCollection<IComponent>();

		/// <summary>
		/// Array of the DataItems collection for COM Interop
		/// </summary>
		public ReadOnlyObservableCollection<IDataItem> DataItems
		{
         get;
         private set;
		}

		/// <summary>
		/// Array of the Components collection for COM Interop
		/// </summary>
		public ReadOnlyObservableCollection<IComponent> Components
		{
         get;
         private set;
		}

      /// <summary>
      /// Creates a new device from an MTConnect XML device node
      /// </summary>
      /// <param name="xElem">The MTConnect XML node which defines the device</param>
      internal Device(XElement xElem = null) 
      {
		DataItems = new ReadOnlyObservableCollection<IDataItem>(_dataItems);
		Components = new ReadOnlyObservableCollection<IComponent>(_components);

		if (xElem?.Name.LocalName == "Device" || xElem?.Name.LocalName == "Agent")
		{
			Model = xElem;
			IsAgent = xElem?.Name.LocalName == "Agent";
			
			// Populate basic fields
			UUID = ParseUtility.GetAttribute(xElem, "uuid");
			Id = ParseUtility.GetAttribute(xElem, "id");
			Name = ParseUtility.GetAttribute(xElem, "name");

			try
			{
				var descXml = xElem.Descendants().First(x => x.Name.LocalName == "Description");

				Description = descXml.Value ?? string.Empty;
				Manufacturer = ParseUtility.GetAttribute(descXml, "manufacturer");
				SerialNumber = ParseUtility.GetAttribute(descXml, "serialNumber");
			}
			catch {}

			_dataItems.AddRange(ParseUtility.GetDataItems(xElem));
			_components.AddRange(ParseUtility.GetComponents(xElem));
		}
      }
      
      public IDataItem GetDataItem(string category, string type, bool topLevel = true)
      {
	      try
	      {
		      return DataItems.Single(di => di.Category == category && di.Type == type);
	      }
	      catch
	      {
		      return null;
	      }
      }

      public IDataItem GetEvent(string type, bool topLevel = true)
      {
	      return GetDataItem("EVENT", type, topLevel);
      }
      
      public (IDataItem,string?) GetEventValue(string type, bool topLevel = true)
      {
	      var di = GetDataItem("EVENT", type, topLevel);
	      return (di, di?.CurrentSample.Value);
      }

      public (IDataItem,bool) IsEventAvailable(string type, bool topLevel = true)
      {
	      var di = GetEventValue(type, topLevel);
	      return (di.Item1, di.Item2 == "AVAILABLE");
      }
	}
}
