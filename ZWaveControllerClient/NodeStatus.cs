namespace ZWaveControllerClient
{
    public enum NodeStatus : byte
    {
        Unknown = 0x00,
        LearnReady = 0x01,
        NodeFound = 0x02,
        AddingRemovingSlave = 0x03,
        AddingRemovingController = 0x04,
        ProtocolDone = 0x05,
        Done = 0x06,
        Failed = 0x07
    }
}
