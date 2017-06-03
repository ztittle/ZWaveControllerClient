namespace ZWaveControllerClient
{
    public enum ApplicationUpdateStatus : byte
    {
        None = 0,
        StaticUpdateControllerId = 0x10,
        DeleteDone = 0x20,
        AddDone = 0x40,
        RoutePending = 0x80,
        NodeInfoReqFailed = 0x81,
        NoneInfoReqDone = 0x82,
        NodeInfoReceived = 0x84
    }
}
