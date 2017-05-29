namespace ZWaveControllerClient.DeviceClasses
{
    public class SpecificDevice
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public string Help { get; internal set; }

        public string Comment { get; internal set; }

        public override string ToString()
        {
            return Help;
        }
    }
}
