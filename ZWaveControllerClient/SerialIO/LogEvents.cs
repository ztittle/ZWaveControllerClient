using Microsoft.Extensions.Logging;

namespace ZWaveControllerClient.SerialIO
{
    public class LogEvents
    {
        public static readonly EventId DispatchFrame = new EventId(1001, nameof(DispatchFrame));
        public static readonly EventId Dispatch = new EventId(1002, nameof(Dispatch));
        public static readonly EventId ResponseReceived = new EventId(1003, nameof(ResponseReceived));
        public static readonly EventId NodeInfoReceived = new EventId(1004, nameof(NodeInfoReceived));
        public static readonly EventId Receive = new EventId(1005, nameof(Receive));
        public static readonly EventId ReceiveFrame = new EventId(1006, nameof(ReceiveFrame));

        public static readonly EventId RequestFrameRetry = new EventId(5001, nameof(RequestFrameRetry));
        public static readonly EventId ProcessFramesException = new EventId(5002, nameof(ProcessFramesException));
        public static readonly EventId ProcessReceivedFrameChecksumFailed = new EventId(5003, nameof(ProcessReceivedFrameChecksumFailed));
        public static readonly EventId RequestCancelled = new EventId(5004, nameof(RequestCancelled));
        public static readonly EventId RequestNotAcknowledged = new EventId(5005, nameof(RequestNotAcknowledged));
        public static readonly EventId NodeInfoFailedToGetStatus = new EventId(5006, nameof(NodeInfoFailedToGetStatus));
        public static readonly EventId SerialPortError = new EventId(5007, nameof(SerialPortError));
    }
}
