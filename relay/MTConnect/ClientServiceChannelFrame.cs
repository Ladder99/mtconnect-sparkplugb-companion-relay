namespace mtc_spb_relay.MTConnect
{
    public class ClientServiceChannelFrame
    {
        public enum FrameTypeEnum
        {
            UNKNOWN,
            ERROR,
            PROBE_COMPLETED,
            PROBE_FAILED,
            CURRENT_COMPLETED,
            CURRENT_FAILED,
            SAMPLE_COMPLETED,
            SAMPLE_FAILED,
            DATA_CHANGED
        }

        public FrameTypeEnum Type { get; set; }
        public dynamic Payload { get; set; }
    }
}