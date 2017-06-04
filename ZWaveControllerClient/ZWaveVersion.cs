namespace ZWaveControllerClient
{
    public class ZWaveVersion
    {
        public string Version { get; internal set; }

        public byte ZWaveProtocolVersion { get; internal set; }

        public byte ZWaveProtocolSubVersion { get; internal set; }

        public byte ZWaveApplicationVersion { get; internal set; }

        public byte ZWaveApplicationSubVersion { get; internal set; }



        public Libraries Library { get; internal set; }
    }
}
