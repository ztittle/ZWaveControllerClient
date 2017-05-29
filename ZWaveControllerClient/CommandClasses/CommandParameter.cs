using System.Collections.Generic;

namespace ZWaveControllerClient.CommandClasses
{
    public class CommandParameter
    {
        public byte Key { get; internal set; }

        public string Name { get; internal set; }

        public ParameterType Type { get; internal set; }

        public byte TypeHashCode { get; internal set; }

        public string Comment { get; internal set; }

        public IReadOnlyCollection<CommandParameterValueAttribute> ValueAttributes { get; internal set; }

        public IReadOnlyCollection<CommandParameterArrayAttribute> ArrayAttributes { get; internal set; }

        public IReadOnlyCollection<CommandParameterBit24> Bit24Collection { get; internal set; }

        public IReadOnlyCollection<CommandParameterBitfield> Bitfields { get; internal set; }

        public IReadOnlyCollection<CommandParameterBitflag> Bitflags { get; internal set; }

        public IReadOnlyCollection<CommandParameterConstant> Constants { get; internal set; }

        public IReadOnlyCollection<CommandParameterDescLoc> DescLocs { get; internal set; }

        public IReadOnlyCollection<CommandParameterDWord> DWords { get; internal set; }

        public IReadOnlyCollection<CommandParameterEnum> Enums { get; internal set; }

        public IReadOnlyCollection<CommandParameterWord> Words { get; internal set; }

        public IReadOnlyCollection<CommandParameterVariant> Variants { get; internal set; }

        public EncapType EncapType { get; internal set; }

        public bool SkipField { get; internal set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
