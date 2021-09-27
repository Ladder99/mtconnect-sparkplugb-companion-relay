using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MTConnectSharp
{
    public class SlimClient: IIMTConnectClient
    {
        private IIMTConnectClient _referenceClient;
        
        public string AgentUri => _referenceClient.AgentUri;
        public string Sender => _referenceClient.Sender;
        public ReadOnlyObservableCollection<IDevice> Devices { get; private set; }
        public IDevice GetAgent()
        {
            return Devices.Single(d => d.IsAgent);
        }

        public SlimClient(IIMTConnectClient client, List<IDevice> devices)
        {
            _referenceClient = client;
            
            Devices = new ReadOnlyObservableCollection<IDevice>(new ObservableCollection<IDevice>(devices));
        }
    }
}