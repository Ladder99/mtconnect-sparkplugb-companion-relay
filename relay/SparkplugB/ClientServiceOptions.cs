using System;

namespace mtc_spb_relay.SparkplugB
{
    public class ClientServiceOptions
    {
        public string BrokerAddress { get; set; }
        public int Port { get; set; }
        public string ClientId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseTls { get; set; }
        public TimeSpan ReconnectInterval { get; set; }
    }
}