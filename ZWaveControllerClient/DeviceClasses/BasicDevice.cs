namespace ZWaveControllerClient.DeviceClasses
{
    public class BasicDevice
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public string Help { get; internal set; }

        public string Comment { get; internal set; }

        public bool IsReadOnly { get; internal set; }

        public override string ToString()
        {
            return Help;
        }
    }
}
