namespace mtc_spb_relay.MTConnect
{
    public class ClientServiceOptions
    {
        public string AgentUri { get; set; }
        public int PollIntervalMs { get; set; }
        public int RetryIntervalMs { get; set; }
        public bool SupressDataItemChangeOnCurrent { get; set; }
    }
}