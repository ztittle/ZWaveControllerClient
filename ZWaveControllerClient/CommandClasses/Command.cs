using System.Collections.Generic;

namespace ZWaveControllerClient.CommandClasses
{
    public class Command
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public string Help { get; internal set; }

        public string Comment { get; internal set; }

        public IReadOnlyCollection<CommandParameter> Parameters { get; internal set; }

        public IReadOnlyCollection<CommandVariantGroup> VariantGroups { get; internal set; }

        public override string ToString()
        {
            return Help;
        }
    }
}
