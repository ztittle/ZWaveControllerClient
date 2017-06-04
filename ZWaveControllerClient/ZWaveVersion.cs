namespace ZWaveControllerClient
{
    public class ZWaveVersion
    {
        public string Version { get; internal set; }

        public byte ProtocolVersion { get; internal set; }

        public byte ProtocolSubVersion { get; internal set; }

        public byte ApplicationVersion { get; internal set; }

        public byte ApplicationSubVersion { get; internal set; }

        public Libraries Library { get; internal set; }

        public override string ToString()
        {
            return Version;
        }
    }
}
