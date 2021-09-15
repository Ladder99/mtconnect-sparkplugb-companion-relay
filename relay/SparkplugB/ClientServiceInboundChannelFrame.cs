namespace mtc_spb_relay.SparkplugB
{
    public class ClientServiceInboundChannelFrame
    {
        public enum FrameTypeEnum
        {
            UNKNOWN,
            ERROR,
            NODE_BIRTH,
            NODE_DEATH
        }

        public FrameTypeEnum Type { get; set; }
        public dynamic Payload { get; set; }
    }
}