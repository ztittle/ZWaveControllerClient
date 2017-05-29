using System.Collections.Generic;

namespace ZWaveControllerClient.CommandClasses
{
    public class CommandVariantGroup
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public byte VariantKey { get; internal set; }

        public byte ParamOffs { get; internal set; }

        public byte SizeMask { get; internal set; }

        public byte SizeOffs { get; internal set; }

        public byte TypeHashCode { get; internal set; }

        public string Comment { get; internal set; }

        public IReadOnlyCollection<CommandParameter> Parameters { get; internal set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
