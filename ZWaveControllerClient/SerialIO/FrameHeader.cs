namespace ZWaveControllerClient.SerialIO
{
    public enum FrameHeader : byte
    {
        /// <summary>
        /// SOF
        /// </summary>
        StartOfFrame = 0x01,
        /// <summary>
        /// ACK
        /// </summary>
        Acknowledged = 0x06,
        /// <summary>
        /// NAK
        /// </summary>
        NotAcknowledged = 0x15,
        /// <summary>
        /// Cancel - Resend request
        /// </summary>
        Cancelled = 0x18
    }
}
