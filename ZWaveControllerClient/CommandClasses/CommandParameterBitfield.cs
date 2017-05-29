namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameterBitfield
    {
        public byte Key { get; internal set; }

        public string FieldName { get; internal set; }

        public byte FieldMask { get; internal set; }

        public int Shifter { get; internal set; }

        public override string ToString()
        {
            return FieldName;
        }
    }
}
