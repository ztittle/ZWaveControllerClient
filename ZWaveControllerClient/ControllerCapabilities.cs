using System;

namespace ZWaveControllerClient
{
    [Flags]
    public enum ControllerCapabilities : byte
    {
        IsSecondary = 0x01,
        OnOtherNetwork = 0x02,
        NodeIdServerPresent = 0x04,
        IsRealPrimary = 0x08,
        IsStaticUpdateController = 0x10,
    }
}
