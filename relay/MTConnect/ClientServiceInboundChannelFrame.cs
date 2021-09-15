namespace mtc_spb_relay.MTConnect
{
    public class ClientServiceInboundChannelFrame
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