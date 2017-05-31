using System;

namespace ZWaveControllerClient
{
    [Flags]
    public enum TransmitOptions : byte
    {
        None = 0x00,
        Acknowledge = 0x01,
        LowPower = 0x02,
        AutoRoute = 0x04,
        NoRoute = 0x10,
        Explore = 0x20,
        NoRetransmit = 0x40,
    }
}
