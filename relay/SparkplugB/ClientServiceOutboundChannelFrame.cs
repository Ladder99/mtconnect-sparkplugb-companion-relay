namespace mtc_spb_relay.SparkplugB
{
    public class ClientServiceOutboundChannelFrame
    {
        public enum FrameTypeEnum
        {
            UNKNOWN,
            ERROR
        }

        public FrameTypeEnum Type { get; set; }
        public dynamic Payload { get; set; }
    }
}