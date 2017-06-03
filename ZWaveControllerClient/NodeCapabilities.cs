using ZWaveControllerClient.DeviceClasses;

namespace ZWaveControllerClient
{
    public class NodeCapabilities
    {
        public BasicDevice BasicType { get; internal set; }

        public GenericDevice GenericType { get; internal set; }
        
        public SpecificDevice SpecificType { get; internal set; }
    }
}
