using System;
using System.Collections.Generic;

namespace ZWaveControllerClient.SerialIO
{
    public interface ILogger
    {
        void LogException(Exception e, string msg = "");
        void LogError(string msg, params KeyValuePair<string, object>[] details);
        void LogInformation(string msg, params KeyValuePair<string, object>[] details);
    }
}
