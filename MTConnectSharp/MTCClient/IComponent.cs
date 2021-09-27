using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace MTConnectSharp
{
   public interface IComponent
	{
		ReadOnlyObservableCollection<IComponent> Components { get; }
		ReadOnlyObservableCollection<IDataItem> DataItems { get; }
		string Type { get; }
		string Id { get; }
		string Name { get; }
		string LongName { get; }
		XElement Model { get; }
	}
}
