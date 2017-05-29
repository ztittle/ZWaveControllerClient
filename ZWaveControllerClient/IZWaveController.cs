using System;
using System.IO;
using System.Threading.Tasks;

namespace ZWaveControllerClient
{
    public interface IZWaveController : IDisposable
    {
        void Connect();
        void Disconnect();
        Task Initialize(Stream xmlCmdClassesStream);
    }
}
