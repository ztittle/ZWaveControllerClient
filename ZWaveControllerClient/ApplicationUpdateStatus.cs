namespace ZWaveControllerClient
{
    public enum ApplicationUpdateStatus : byte
    {
        NONE = 0,
        SUC_ID = 0x10,
        DELETE_DONE = 0x20,
        ADD_DONE = 0x40,
        ROUTING_PENDING = 0x80,
        NODE_INFO_REQ_FAILED = 0x81,
        NODE_INFO_REQ_DONE = 0x82,
        NODE_INFO_RECEIVED = 0x84
    }
}
