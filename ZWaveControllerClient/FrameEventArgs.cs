using System;

namespace ZWaveControllerClient
{ 
    public class FrameEventArgs : EventArgs
    {
        public Frame Frame { get; internal set; }
    }
}
