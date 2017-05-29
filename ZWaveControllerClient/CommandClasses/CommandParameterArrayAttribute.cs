namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterArrayAttribute
    {
        public byte Key { get; internal set; }

        public uint Length { get; internal set; }

        public bool IsAscii { get; internal set; }

        public bool ShowHex { get; internal set; }
    }
}
