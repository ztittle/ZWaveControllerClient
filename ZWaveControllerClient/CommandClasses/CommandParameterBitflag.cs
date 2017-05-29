namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterBitflag
    {
        public byte Key { get; internal set; }

        public string FlagName { get; internal set; }

        public byte FlagMask { get; internal set; }

        public override string ToString()
        {
            return FlagName;
        }
    }
}
