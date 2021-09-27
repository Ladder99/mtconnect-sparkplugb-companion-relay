using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;

namespace MTConnectSharp
{
    public class SlimComponent: MTCItemBase, IComponent
    {
        private List<IDataItem> _referenceDataItems;
        private IComponent _referenceComponent;

        private ObservableCollection<IDataItem> _dataItems;
        private ObservableCollection<IComponent> _components;
        
        public ReadOnlyObservableCollection<IComponent> Components { get; private set; }

        public ReadOnlyObservableCollection<IDataItem> DataItems { get; }

        public string Type => _referenceComponent.Type;
        public XElement Model => _referenceComponent.Model;

        internal SlimComponent(IComponent component, List<IDataItem> dataItems)
        {
            _referenceComponent = component;
            _referenceDataItems = dataItems;

            Id = _referenceComponent.Id;
            Name = _referenceComponent.Name;

            _dataItems = new ObservableCollection<IDataItem>(
                _referenceComponent.DataItems
                    .Where(di => _referenceDataItems.Any(rdi => rdi.Id == di.Id)));
            DataItems = new ReadOnlyObservableCollection<IDataItem>(_dataItems);

            _components = new ObservableCollection<IComponent>();
            foreach (var referenceComponent in _referenceComponent.Components)
            {
                _components.Add(new SlimComponent(referenceComponent, _referenceDataItems));
            }
            Components = new ReadOnlyObservableCollection<IComponent>(_components);
        }
    }
}