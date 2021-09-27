using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace MTConnectSharp
{
   public interface IDevice
	{
		ReadOnlyObservableCollection<IComponent> Components { get; }
		ReadOnlyObservableCollection<IDataItem> DataItems { get; }
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
		(IDataItem, string?) GetEventValue(string type, bool topLevel = true);
		(IDataItem, bool) IsEventAvailable(string type, bool topLevel = true);
	}
}
