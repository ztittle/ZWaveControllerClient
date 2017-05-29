namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterWord
    {
        public byte Key { get; internal set; }

        public bool HasDefines { get; internal set; }

        public bool ShowHex { get; internal set; }
    }
}
