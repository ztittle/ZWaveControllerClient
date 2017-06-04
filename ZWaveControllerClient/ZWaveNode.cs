using System.Collections.Generic;
using ZWaveControllerClient.CommandClasses;

namespace ZWaveControllerClient
{

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
            return $"{Id}: {ProtocolInfo.SpecificType}";
        }
    }
}
