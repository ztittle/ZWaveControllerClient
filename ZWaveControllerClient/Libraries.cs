namespace ZWaveControllerClient
{
    public enum Libraries : byte
    {
        NoLib = 0x00,
        ControllerStaticLib = 0x01,
        ControllerPortableLib = 0x02,
        SlaveEnhancedLib = 0x03,
        SlaveLib = 0x04,
        InstallerLib = 0x05,
        SlaveRoutingLib = 0x06,
        ControllerBridgeLib = 0x07
    }
}
