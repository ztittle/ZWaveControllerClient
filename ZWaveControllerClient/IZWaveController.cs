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

        Task<ZWaveNode> AddNode(CancellationToken cancellationToken, ZWaveMode mode = ZWaveMode.NodeAny);
        Task<IReadOnlyCollection<ZWaveNode>> AddNodeNetworkWideInclusion(CancellationToken cancellationToken);
        void Connect();
        void Disconnect();
        Task DiscoverNodes(CancellationToken cancellationToken);
        void DispatchFrame(Frame frame);
        void Dispatch(params byte[] bytes);
        Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(Frame frame);
        Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(CancellationToken cancellationToken, Frame frame);
        Task FetchControllerInfo(CancellationToken cancellationToken);
        Task FetchControllerVersion(CancellationToken cancellationToken);
        Task FetchSerialCapabilities(CancellationToken cancellationToken);
        Task FetchControllerCapabilities(CancellationToken cancellationToken);
        Task FetchMemoryId(CancellationToken cancellationToken);
        Task FetchSucNodeId(CancellationToken cancellationToken);
        Task FetchNodeInfo(CancellationToken cancellationToken);
        Task FetchNodeInfo(CancellationToken cancellationToken, ZWaveNode node);
        Task<ZWaveNode> RemoveNode(CancellationToken cancellationToken);
        Task<IReadOnlyCollection<Frame>> SendCommand(byte nodeId, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes);
        Task<IReadOnlyCollection<Frame>> SendCommand(ZWaveNode node, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes);
    }
}
