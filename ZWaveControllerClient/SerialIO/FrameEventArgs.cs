using System;

namespace ZWaveControllerClient.SerialIO
{ 
    public class FrameEventArgs : EventArgs
    {
        public Frame Frame { get; internal set; }
    }
}
