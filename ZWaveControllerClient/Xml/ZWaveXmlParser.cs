using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ZWaveControllerClient.CommandClasses;
using ZWaveControllerClient.DeviceClasses;

namespace ZWaveControllerClient.Xml
{
    public class ZWaveClassesXmlParser
    {
        public ZWaveClasses Parse(Stream xmlStream)
        {
            var xmlDoc = XDocument.Load(xmlStream);

            var zwClasses = new ZWaveClasses();

            zwClasses.BasicDevices = xmlDoc.Root.Elements("bas_dev")
                .Select(AsBasicDevice).ToList().AsReadOnly();

            zwClasses.GenericDevices = xmlDoc.Root.Elements("gen_dev")
                .Select(AsGenericDevice).ToList().AsReadOnly();

            zwClasses.CommandClasses = xmlDoc.Root.Elements("cmd_class")
                .Select(AsCommandClass).ToList().AsReadOnly();

            return zwClasses;
        }

        private BasicDevice AsBasicDevice(XElement node)
        {
            return new BasicDevice
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Help = node.Attribute("help")?.Value,
                IsReadOnly = node.Attribute("read_only").AsBoolean().GetValueOrDefault(),
                Comment = node.Attribute("comment")?.Value
            };
        }

        private GenericDevice AsGenericDevice(XElement node)
        {
            return new GenericDevice
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Help = node.Attribute("help")?.Value,
                Comment = node.Attribute("comment")?.Value,
                SpecificDevices = node.Elements("spec_dev").Select(AsSpecificDevice).ToList().AsReadOnly()
            };
        }

        private SpecificDevice AsSpecificDevice(XElement node)
        {
            return new SpecificDevice
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Help = node.Attribute("help")?.Value,
                Comment = node.Attribute("comment")?.Value
            };
        }

        private CommandClass AsCommandClass(XElement node)
        {
            return new CommandClass
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Help = node.Attribute("help")?.Value,
                IsReadOnly = node.Attribute("read_only").AsBoolean(),
                Comment = node.Attribute("comment")?.Value,
                Version = node.Attribute("version").Value,
                Commands = node.Elements("cmd").Select(AsCommand).ToList().AsReadOnly()
            };
        }

        private Command AsCommand(XElement node)
        {
            return new Command
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Help = node.Attribute("help")?.Value,
                Comment = node.Attribute("comment")?.Value,
                Parameters = node.Elements("param").Select(AsParameter).ToList().AsReadOnly(),
                VariantGroups = node.Elements("variant_group").Select(AsVariantGroup).ToList().AsReadOnly()
            };
        }

        private CommandParameter AsParameter(XElement node)
        {
            return new CommandParameter
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                TypeHashCode = node.Attribute("typehashcode").AsByte().GetValueOrDefault(),
                Comment = node.Attribute("comment")?.Value,
                EncapType = node.Attribute("encaptype").AsEncapType().GetValueOrDefault(),
                Type = node.Attribute("type").AsParameterType().GetValueOrDefault(),
                SkipField = node.Attribute("skipfield").AsBoolean().GetValueOrDefault(),
                ArrayAttributes = node.Elements("arrayattrib").Select(AsArrayAttribute).ToList().AsReadOnly(),
                Bit24Collection = node.Elements("bit_24").Select(AsBit24).ToList().AsReadOnly(),
                Bitfields = node.Elements("bitfield").Select(AsBitfield).ToList().AsReadOnly(),
                Constants = node.Elements("const").Select(AsConstant).ToList().AsReadOnly(),
                Bitflags = node.Elements("bitflag").Select(AsBitflag).ToList().AsReadOnly(),
                DescLocs = node.Elements("paramdescloc").Select(AsDescloc).ToList().AsReadOnly(),
                DWords = node.Elements("dword").Select(AsDword).ToList().AsReadOnly(),
                Enums = node.Elements("word").Select(AsEnum).ToList().AsReadOnly(),
                ValueAttributes = node.Elements("valueattrib").Select(AsValueAttribute).ToList().AsReadOnly(),
                Variants = node.Elements("variant").Select(AsVariant).ToList().AsReadOnly(),
                Words = node.Elements("word").Select(AsWord).ToList().AsReadOnly()
            };
        }

        private CommandVariantGroup AsVariantGroup(XElement node)
        {
            return new CommandVariantGroup
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
                Comment = node.Attribute("comment")?.Value,
                ParamOffs = node.Attribute("paramOffs").AsByte().GetValueOrDefault(),
                SizeMask = node.Attribute("sizemask").AsByte().GetValueOrDefault(),
                SizeOffs = node.Attribute("sizeoffs").AsByte().GetValueOrDefault(),
                TypeHashCode = node.Attribute("typehashcode").AsByte().GetValueOrDefault(),
                VariantKey = node.Attribute("variantKey").AsByte().GetValueOrDefault(),
                Parameters = node.Elements("parameter").Select(AsParameter).ToList().AsReadOnly()
            };
        }

        private CommandParameterWord AsWord(XElement node)
        {
            return new CommandParameterWord
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                HasDefines = node.Attribute("hasdefines").AsBoolean().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault()
            };
        }

        private CommandParameterVariant AsVariant(XElement node)
        {
            return new CommandParameterVariant
            {
                IsSigned = node.Attribute("issigned").AsBoolean().GetValueOrDefault(),
                ParamOffs = node.Attribute("paramoffs").AsInteger().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault(),
                SizeMask = node.Attribute("sizemask").AsByte().GetValueOrDefault(),
                SizeOffs = node.Attribute("sizeoffs").AsInteger().GetValueOrDefault()
            };
        }

        private CommandParameterValueAttribute AsValueAttribute(XElement node)
        {
            return new CommandParameterValueAttribute
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                HasDefines = node.Attribute("hasdefines").AsBoolean().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault()
            };
        }

        private CommandParameterEnum AsEnum(XElement node)
        {
            return new CommandParameterEnum
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Name = node.Attribute("name")?.Value,
            };
        }

        private CommandParameterDWord AsDword(XElement node)
        {
            return new CommandParameterDWord
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                HasDefines = node.Attribute("hasdefines").AsBoolean().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault()
            };
        }

        private CommandParameterDescLoc AsDescloc(XElement node)
        {
            return new CommandParameterDescLoc
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                Param = node.Attribute("param").AsInteger().GetValueOrDefault(),
                ParamDesc = node.Attribute("paramdesc").AsInteger().GetValueOrDefault(),
                ParamStart = node.Attribute("paramstart").AsInteger().GetValueOrDefault()
            };
        }

        private CommandParameterBitflag AsBitflag(XElement node)
        {
            return new CommandParameterBitflag
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                FlagMask = node.Attribute("flagmask").AsByte().GetValueOrDefault(),
                FlagName = node.Attribute("flagname")?.Value
            };
        }

        private CommandParameterConstant AsConstant(XElement node)
        {
            return new CommandParameterConstant
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                FlagMask = node.Attribute("flagmask").AsByte().GetValueOrDefault(),
                FlagName = node.Attribute("flagname")?.Value
            };
        }

        private CommandParameterBitfield AsBitfield(XElement node)
        {
            return new CommandParameterBitfield
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                FieldMask = node.Attribute("fieldmask").AsByte().GetValueOrDefault(),
                FieldName = node.Attribute("fieldname")?.Value,
                Shifter = node.Attribute("shifter").AsInteger().GetValueOrDefault()
            };
        }

        private CommandParameterBit24 AsBit24(XElement node)
        {
            return new CommandParameterBit24
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                HasDefines = node.Attribute("hasdefines").AsBoolean().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault()
            };
        }

        private CommandParameterArrayAttribute AsArrayAttribute(XElement node)
        {
            return new CommandParameterArrayAttribute
            {
                Key = node.Attribute("key").AsByte().GetValueOrDefault(),
                IsAscii = node.Attribute("is_ascii").AsBoolean().GetValueOrDefault(),
                Length = (uint)node.Attribute("len").AsInteger().GetValueOrDefault(),
                ShowHex = node.Attribute("showhex").AsBoolean().GetValueOrDefault()
            };
        }
    }

    internal static class XAttributeExtensions
    {
        public static bool? AsBoolean(this XAttribute attr)
        {
            return attr == null ? (bool?)null : bool.Parse(attr.Value);
        }

        public static byte? AsByte(this XAttribute attr)
        {
            return attr == null ? (byte?)null : byte.Parse(attr.Value.Substring(2), NumberStyles.AllowHexSpecifier);
        }

        public static int? AsInteger(this XAttribute attr)
        {
            return attr == null ? (int?)null : int.Parse(attr.Value);
        }

        public static ParameterType? AsParameterType(this XAttribute attr)
        {
            switch (attr?.Value)
            {
                case "ARRAY":
                    return ParameterType.Array;
                case "BIT_24":
                    return ParameterType.Bit24;
                case "BITMASK":
                    return ParameterType.Bitmask;
                case "BYTE":
                    return ParameterType.Byte;
                case "CONSTANT":
                    return ParameterType.Constant;
                case "DWORD":
                    return ParameterType.DWord;
                case "ENUM":
                    return ParameterType.Enum;
                case "ENUM_ARRAY":
                    return ParameterType.EnumArray;
                case "MARKER":
                    return ParameterType.Marker;
                case "MULTI_ARRAY":
                    return ParameterType.MultipleArray;
                case "STRUCT_BYTE":
                    return ParameterType.StructByte;
                case "VARIANT":
                    return ParameterType.Variant;
                case "WORD":
                    return ParameterType.Word;
                default:
                    return null;
            }
        }

        public static EncapType? AsEncapType(this XAttribute attr)
        {
            switch (attr?.Value)
            {
                case "CMD_CLASS_REF":
                    return EncapType.CMD_CLASS_REF;
                case "CMD_DATA":
                    return EncapType.CMD_DATA;
                case "CMD_ENCAP":
                    return EncapType.CMD_ENCAP;
                case "CMD_REF":
                    return EncapType.CMD_REF;
                case "GEN_DEV_REF":
                    return EncapType.GEN_DEV_REF;
                case "NODE_NUMBER":
                    return EncapType.NODE_NUMBER;
                case "SPEC_DEV_REF":
                    return EncapType.SPEC_DEV_REF;
                default:
                    return null;
            }
        }
    }
}
