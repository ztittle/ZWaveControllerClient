namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterEnum
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
