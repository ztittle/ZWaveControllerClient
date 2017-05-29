using System.Collections.Generic;
using ZWaveControllerClient.CommandClasses;
using ZWaveControllerClient.DeviceClasses;

namespace ZWaveControllerClient
{
    public class ZWaveClasses
    {
        public IReadOnlyCollection<BasicDevice> BasicDevices { get; internal set; }

        public IReadOnlyCollection<GenericDevice> GenericDevices { get; internal set; }

        public IReadOnlyCollection<CommandClass> CommandClasses { get; internal set; }
    }
}
