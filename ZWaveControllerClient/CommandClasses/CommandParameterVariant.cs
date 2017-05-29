namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterVariant
    {
        public int ParamOffs { get; internal set; }

        public bool ShowHex { get; internal set; }

        public bool IsSigned { get; internal set; }

        public byte SizeMask { get; internal set; }

        public int SizeOffs { get; internal set; }
    }
}
