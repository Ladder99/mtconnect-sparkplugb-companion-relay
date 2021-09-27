using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MTConnectSharp
{
    public class SlimDevice : MTCItemBase, IDevice
    {
        private List<IDataItem> _referenceDataItems;
        private IDevice _referenceDevice;
        
        public string UUID => _referenceDevice.UUID;
        public string Description => _referenceDevice.Description;
        public string Manufacturer => _referenceDevice.Manufacturer;
        public string SerialNumber => _referenceDevice.SerialNumber;
        public bool IsAgent => _referenceDevice.IsAgent;
        public XElement Model => _referenceDevice.Model;
        
        private ObservableCollection<IDataItem> _dataItems = new ObservableCollection<IDataItem>();
        
        private ObservableCollection<IComponent> _components = new ObservableCollection<IComponent>();
        public ReadOnlyObservableCollection<IDataItem> DataItems { get; private set; }
        public ReadOnlyObservableCollection<IComponent> Components { get; private set; }
        
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

        public SlimDevice(IDevice device, List<IDataItem> dataItems)
        {
            _referenceDataItems = dataItems;
            _referenceDevice = device;

            Id = _referenceDevice.Id;
            Name = _referenceDevice.Name;
            
            _dataItems = new ObservableCollection<IDataItem>(
                _referenceDevice.DataItems
                    .Where(di => _referenceDataItems.Any(rdi => rdi.Id == di.Id)));
            DataItems = new ReadOnlyObservableCollection<IDataItem>(_dataItems);
            
            _components = new ObservableCollection<IComponent>();
            foreach (var referenceComponent in _referenceDevice.Components)
            {
                _components.Add(new SlimComponent(referenceComponent, _referenceDataItems));
            }
            Components = new ReadOnlyObservableCollection<IComponent>(_components);
        }
    }
}