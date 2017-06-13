using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ZWaveControllerClient.CommandClasses;

namespace ZWaveControllerClient
{
    public delegate void FrameReceivedEventHandler(object sender, FrameEventArgs e);
    public interface IZWaveController : IDisposable
    {
        event FrameReceivedEventHandler FrameReceived;

        IReadOnlyList<ZWaveNode> Nodes { get; }
        ZWaveClasses Classes { get; }
        byte ChipType { get; }
        byte ChipRevision { get; }
        byte SerialApiVersion { get; }
        bool IsSlaveApi { get; }
        byte SequenceNumber { get; }
        ControllerCapabilities Capabilities { get; }
        ZWaveVersion Version { get; }
        byte SucNodeId { get; }
        byte Id { get; }
        byte[] HomeId { get; }

        Task<ZWaveNode> AddNode(ZWaveMode mode = ZWaveMode.NodeAny);
        Task<IReadOnlyCollection<ZWaveNode>> AddNodeNetworkWideInclusion(CancellationToken cancellationToken);
        void Connect();
        void Disconnect();
        Task DiscoverNodes();
        void DispatchFrame(Frame frame);
        void Dispatch(params byte[] bytes);
        Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(Frame frame);
        Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(Frame frame, CancellationToken cancellationToken);
        Task FetchControllerInfo();
        Task FetchNodeInfo();
        Task FetchNodeInfo(ZWaveNode node);
        Task<ZWaveNode> RemoveNode();
        Task<IReadOnlyCollection<Frame>> SendCommand(byte nodeId, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes);
        Task<IReadOnlyCollection<Frame>> SendCommand(ZWaveNode node, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes);
    }
}
