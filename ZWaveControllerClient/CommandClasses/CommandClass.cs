using System.Collections.Generic;

namespace ZWaveControllerClient.CommandClasses
{
    public class CommandClass
    {
        public byte Key { get; internal set; }

        public string Version { get; internal set; }

        public string Name { get; internal set; }

        public string Help { get; internal set; }

        public bool? IsReadOnly { get; internal set; }

        public string Comment { get; internal set; }

        public IReadOnlyCollection<Command> Commands { get; internal set; }

        public override string ToString()
        {
            return $"{Help} (v{Version})";
        }
    }
}
