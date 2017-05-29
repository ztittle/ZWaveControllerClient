namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterDescLoc
    {
        public byte Key { get; internal set; }

        public int Param { get; internal set; }

        public int ParamDesc { get; internal set; }

        public int ParamStart { get; internal set; }
    }
}
