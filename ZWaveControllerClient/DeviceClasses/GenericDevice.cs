using System.Collections.Generic;

namespace ZWaveControllerClient.DeviceClasses
{
    public class GenericDevice
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public string Help { get; internal set; }

        public string Comment { get; internal set; }

        public IReadOnlyCollection<SpecificDevice> SpecificDevices { get; internal set; }

        public override string ToString()
        {
            return Help;
        }
    }
}
