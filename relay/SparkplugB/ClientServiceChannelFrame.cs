namespace mtc_spb_relay.SparkplugB
{
    public class ClientServiceChannelFrame
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