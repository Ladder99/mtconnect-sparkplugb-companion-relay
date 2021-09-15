using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace MTConnectSharp
{
   public interface IDevice
	{
		ReadOnlyObservableCollection<Component> Components { get; }
		ReadOnlyObservableCollection<DataItem> DataItems { get; }
		string UUID { get; }
		string Description { get; }
		string Manufacturer { get; }
		string SerialNumber { get; }
		string Id { get; }
		string Name { get; }
		string LongName { get; }
		bool IsAgent { get; }
		XElement Model { get; }
		IDataItem GetDataItem(string category, string type, bool topLevel = true);
		IDataItem GetEvent(string type, bool topLevel = true);
		string GetEventValue(string type, bool topLevel = true);
		bool IsEventAvailable(string type, bool topLevel = true);
	}
}
