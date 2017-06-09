using System;

namespace ZWaveControllerClient
{
    [Flags]
    public enum ZWaveMode : byte
    {
        None = 0x00,
        NodeAny = 0x01,
        NodeController = 0x02,
        NodeSlave = 0x03,
        NodeExisting = 0x04,
        NodeStop = 0x05,
        NodeStopFailed = 0x06,
        NodeOptionNetworkWide = 0x40,
        NodeOptionHighPower = 0x80,
    }
}
