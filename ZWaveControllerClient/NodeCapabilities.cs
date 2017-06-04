using ZWaveControllerClient.DeviceClasses;

namespace ZWaveControllerClient
{
    public class NodeCapabilities
    {
        public byte Reserved { get; internal set; }

        public byte Security { get; internal set; }

        public byte Capability { get; internal set; }

        public bool IsListening => (Capability & 128) != 0;

        public BasicDevice BasicType { get; internal set; }

        public GenericDevice GenericType { get; internal set; }
        
        public SpecificDevice SpecificType { get; internal set; }
    }
}
