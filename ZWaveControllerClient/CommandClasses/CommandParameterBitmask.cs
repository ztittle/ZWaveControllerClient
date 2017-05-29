namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterBitmask
    {
        public byte Key { get; internal set; }

        public int ParamOffs { get; internal set; }

        public int LenMask { get; internal set; }

        public int LenOffs { get; internal set; }
    }
}
