using System.Collections.Generic;
using ZWaveControllerClient.CommandClasses;
using ZWaveControllerClient.DeviceClasses;

namespace ZWaveControllerClient
{
    public class NodeCapabilities
    {
        public BasicDevice BasicType { get; internal set; }

        public GenericDevice GenericType { get; internal set; }
        
        public SpecificDevice SpecificType { get; internal set; }
    }
    
    public class ZWaveNode
    {
        public ZWaveNode()
        {
            ProtocolInfo = new NodeCapabilities();
            SupportedCommandClasses = new List<CommandClass>();
        }

        public byte Id { get; internal set; }

        public ApplicationUpdateStatus UpdateStatus { get; internal set; }

        public NodeCapabilities ProtocolInfo { get; private set; }

        public List<CommandClass> SupportedCommandClasses { get; internal set; }

        public override string ToString()
        {
            return ProtocolInfo.SpecificType.ToString();
        }
    }
}
