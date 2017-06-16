using Microsoft.Extensions.Logging;
using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZWaveControllerClient.CommandClasses;
using ZWaveControllerClient.Xml;

namespace ZWaveControllerClient.Serial
{
    /// <summary>
    /// See https://github.com/yepher/RaZBerry for serial IO notes.
    /// </summary>
    public class ZWaveSerialController : IZWaveController
    {
        private string _portName;
        private SerialPortStream _serialPort;
        private ILogger _logger;
        private ZWaveClasses _zwClasses;
		private readonly Queue<TaskState> _sendTasks = new Queue<TaskState>();
        private byte[] _dataReceivedBuffer = { };
        private const int RetryCount = 2;
        private const int RetryDurationMs = 1100;
        private const int RequestTimeoutMs = 3000;

        private List<ZWaveNode> _nodes = new List<ZWaveNode>();

        public event FrameReceivedEventHandler FrameReceived;

        public IReadOnlyList<ZWaveNode> Nodes => _nodes;

        public ZWaveClasses Classes => _zwClasses;
        public ZWaveVersion Version { get; private set; }
        public byte ChipType { get; private set; }
        public byte ChipRevision { get; private set; }
        public byte SerialApiVersion { get; private set; }
        public bool IsSlaveApi { get; private set; }
        public ControllerCapabilities Capabilities { get; private set; }

        private class TaskState
        {
            public TaskState()
            {
                Responses = new List<Frame>();
            }

            public Frame RequestFrame { get; set; }
            public TaskCompletionSource<IReadOnlyCollection<Frame>> CompletionSource { get; set; }
            public List<Frame> Responses { get; }
            public bool ExpectsMultiResponse
            {
                get
                {
                    switch (RequestFrame.Function)
                    {
                        case ZWaveFunction.SendData:
                        case ZWaveFunction.SendDataMeta:
                        case ZWaveFunction.SendDataMetaMr:
                        case ZWaveFunction.SendDataMr:
                        case ZWaveFunction.SendDataMulti:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public void SetCompleted()
            {
                CompletionSource.SetResult(Responses);
            }
        }

        private byte _sequenceNumber = 1;
        public byte SequenceNumber
        {
            get => _sequenceNumber;
            private set
            {
                if (value == byte.MinValue || value == sbyte.MaxValue)
                {
                    _sequenceNumber = 1;
                }
                else _sequenceNumber = value;
            }
        }

        public byte SucNodeId { get; private set; }
        public byte[] HomeId { get; private set; }
        public byte Id { get; private set; }

        private object _locker = new object();

        public ZWaveSerialController(string portName, ZWaveClasses zwaveClasses, ILoggerFactory loggerFactory)
        {
            _portName = portName;
            _zwClasses = zwaveClasses;
            _logger = loggerFactory.CreateLogger<ZWaveSerialController>();

            _serialPort = new SerialPortStream(_portName);

            _serialPort.DataReceived += _serialPort_DataReceived;
            _serialPort.ErrorReceived += _serialPort_ErrorReceived;

            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;

            Version = new ZWaveVersion();
        }

        public ZWaveSerialController(string portName, Stream xmlCmdClassesStream, ILoggerFactory loggerFactory)
            : this(portName, new ZWaveClassesXmlParser().Parse(xmlCmdClassesStream), loggerFactory)
        {
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			var frameDataLength = _serialPort.BytesToRead;
            if (frameDataLength > 0)
            {
                var frameData = new byte[frameDataLength];

                while (_serialPort.Read(frameData, 0, frameDataLength) <= 0) {}

                _logger.LogTrace(LogEvents.Receive, "Recv bytes: {bytes}", string.Concat(frameData.Select(b => b.ToString("x02"))));
                _dataReceivedBuffer = _dataReceivedBuffer.Concat(frameData).ToArray();
            }

            var frames = CollectFrames();
            if (frames.Count > 0)
			{
				ProcessFrames(frames);                
            }
        }

        public IReadOnlyCollection<Frame> CollectFrames()
        {
            var frames = new List<Frame>();
            while (_dataReceivedBuffer.Length > 0)
            {
				var responseFrame = new Frame(_dataReceivedBuffer);

				var frameLength = responseFrame.Length;
				if (responseFrame.Header == FrameHeader.StartOfFrame)
				{
					frameLength += 2;
				}

                if (_dataReceivedBuffer.Length < frameLength)
                {
                    // need to wait until more data arrives
                    break;
                }

                _logger.LogInformation(LogEvents.ReceiveFrame, "Recv frame: {responseFrame}", responseFrame);

                _dataReceivedBuffer = _dataReceivedBuffer.Skip(frameLength).ToArray();

                frames.Add(responseFrame);
            }

            return frames;
        }

        public void Connect()
        {
            _serialPort.Open();
        }

        public void Disconnect()
        {
            _serialPort.Close();
        }

        private static Frame Ack = new Frame(FrameType.Request, FrameHeader.Acknowledged);

        private static Frame Nack = new Frame(FrameType.Request, FrameHeader.NotAcknowledged);

        public Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(Frame frame)
        {
            try
            {
                using (_requestCancellationTokenSource = new CancellationTokenSource())
                {
                    return DispatchFrameAsync(frame, _requestCancellationTokenSource.Token);
                }
            }
            finally
            {
                _requestCancellationTokenSource = null;
            }
        }

        public async Task<IReadOnlyCollection<Frame>> DispatchFrameAsync(Frame frame, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<IReadOnlyCollection<Frame>>();

            var taskState = new TaskState
            {
                RequestFrame = frame,
                CompletionSource = tcs
            };

            _sendTasks.Enqueue(taskState);
            
            ResetRequestRetryCount();

            using (cancellationToken.Register(state =>
             {
                 var tcsFromState = (TaskState)state;
                 if (_sendTasks.Count > 0 && _sendTasks.Peek() == tcsFromState)
                 {
                     _retransTimer?.Dispose();
                     _retransTimer = null;
                     _sendTasks.Dequeue();
                     tcsFromState.CompletionSource.SetCanceled();
                 }
             }, taskState))
            {
                lock (_locker)
                {
                    if (_sendTasks.Count > 1)
                    {
                        _sendTasks.Peek().CompletionSource.Task.Wait();
                    }
                }

                _retransTimer = new Timer(state =>
                {
                    var requestFrame = (Frame)state;
                    if (_retriesLeft-- == 0)
                    {
                        _logger.LogWarning(LogEvents.RequestFrameRetry, "No retries left. Trying to set cancellation timer. Request frame {requestFrame}", requestFrame);
                        _requestCancellationTokenSource?.CancelAfter(RequestTimeoutMs);
                        _retransTimer.Dispose();
                    }
                    else
                    {
                        _logger.LogWarning(LogEvents.RequestFrameRetry, "Resending frame {requestFrame}, retries left: {retriesLeft}.", requestFrame, _retriesLeft);
                        DispatchFrame(requestFrame);
                    }
                }, frame, RetryDurationMs, RetryDurationMs);

                DispatchFrame(frame);

                return await tcs.Task;
            }            
        }

        public void DispatchFrame(Frame frame)
        {
            _logger.LogInformation(LogEvents.DispatchFrame, "Dispatch frame {frame}, retries left: {_retriesLeft}.", frame, _retriesLeft);
            Dispatch(frame.GetData());
        }

        public void Dispatch(params byte[] bytes)
        {
            _logger.LogTrace(LogEvents.Dispatch, "Send bytes: {bytes}", string.Concat(bytes.Select(b => b.ToString("x02"))));
            _serialPort.Write(bytes, 0, bytes.Length);
            _serialPort.Flush();
        }

        public Task<IReadOnlyCollection<Frame>> SendCommand(byte nodeId, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes)
        {
            return DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.SendData,
               new byte[] { nodeId, (byte)(bytes.Length + 2), commandClass.Key, command.Key }
               .Concat(bytes)
               .Concat(new byte[] { (byte)(transmitOptions), SequenceNumber++ }).ToArray()));
        }

        public Task<IReadOnlyCollection<Frame>> SendCommand(ZWaveNode node, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes)
        {
            return SendCommand(node.Id, commandClass, command, transmitOptions, bytes);
        }

        private void ProcessFrames(IEnumerable<Frame> responseFrames)
        {
            try
            {
                TaskState lastSendTask = null;
                Frame lastRequestFrame = null;

                if (_sendTasks.Count > 0)
                {
                    lastSendTask = _sendTasks.Peek();
                    lastRequestFrame = lastSendTask.RequestFrame;
                }

                foreach (var responseFrame in responseFrames)
                {
                    if (responseFrame.Header == FrameHeader.StartOfFrame)
                    {
                        ProcessReceivedFrame(responseFrame);

                        if (lastRequestFrame?.Function == responseFrame.Function &&
                            responseFrame.IsChecksumValid)
                        {
                            lastSendTask.Responses.Add(responseFrame);
                            
                            if (lastSendTask.ExpectsMultiResponse)
                            {
                                if (responseFrame.Type == FrameType.Request)
                                {
                                    CompleteTask(lastSendTask);
                                }
                            }
                            else
                            {
                                CompleteTask(lastSendTask);
                            }
                        }
                    }
                    else
                    {
                        if (responseFrame.Header == FrameHeader.Cancelled)
                        {
                            _logger.LogWarning(LogEvents.RequestCancelled, "Request was cancelled. Request frame {lastRequestFrame}. Response frame {responseFrame}", lastRequestFrame, responseFrame);
                        }
                        else if (responseFrame.Header == FrameHeader.NotAcknowledged)
                        {
                            _logger.LogWarning(LogEvents.RequestNotAcknowledged, "Request was not acknowledged. Request frame {lastRequestFrame}. Response frame {responseFrame}", lastRequestFrame, responseFrame);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(LogEvents.ProcessFramesException, e, e.Message);
            }
        }

        private void CompleteTask(TaskState lastSendTask)
        {
            _retransTimer?.Dispose();
            _retransTimer = null;
            _sendTasks.Dequeue();
            lastSendTask.SetCompleted();
            _logger.LogInformation(LogEvents.ResponseReceived, "Set result for {lastRequestFrame}", lastSendTask.RequestFrame);
        }

        private void ProcessReceivedFrame(Frame responseFrame)
        {
            ResetRequestRetryCount();
            if (responseFrame.IsChecksumValid)
            {
                DispatchFrame(Ack);
            }
            else
            {
                _logger.LogWarning(LogEvents.ProcessReceivedFrameChecksumFailed, "Checksum failed. Response frame {responseFrame}", responseFrame);
                DispatchFrame(Nack);
                return;
            }

            switch (responseFrame.Function)
            {
                case ZWaveFunction.ApplicationUpdate:
                    HandleApplicationUpdate(responseFrame);
                    break;
            }

            FrameReceived?.Invoke(this, new FrameEventArgs { Frame = responseFrame });
        }

        private void ResetRequestRetryCount()
        {
            _retriesLeft = RetryCount;
            _retransTimer?.Change(RetryDurationMs, RetryDurationMs);
        }

        private void HandleApplicationUpdate(Frame responseFrame)
        {
            var status = (ApplicationUpdateStatus)responseFrame.Payload[0];

            if (status == ApplicationUpdateStatus.NodeInfoReqFailed)
            {
                _logger.LogWarning(LogEvents.NodeInfoFailedToGetStatus, "failed to get node status. Response frame {responseFrame}. Status: {status}", responseFrame, status);
                return;
            }

            var nodeId = responseFrame.Payload[1];
            var znode = _nodes.Single(n => n.Id == nodeId);
            znode.UpdateStatus = status;
            if (status == ApplicationUpdateStatus.NodeInfoReceived)
            {
                var nifLength = responseFrame.Payload[2];
                if (nifLength > 0)
                {
                    var nodeInfo = responseFrame.Payload.Skip(3).Take(nifLength).ToArray();

                    znode.SupportedCommandClasses = nodeInfo.SelectMany(key => _zwClasses.CommandClasses.Where(c => c.Key == key)).ToList();
                    _logger.LogInformation(LogEvents.NodeInfoReceived, "Node: {znode} - Command Classes: {cmdClasses}", znode, string.Join(", ", znode.SupportedCommandClasses.Select(cc => cc.ToString())));
                }
            }
        }

        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            _logger.LogError(LogEvents.SerialPortError, "event {e} type {eventType}", e, e.EventType);
        }
        
        private bool disposedValue = false; // To detect redundant calls
        private int _retriesLeft;
        private Timer _retransTimer;
        private CancellationTokenSource _requestCancellationTokenSource;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _serialPort.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public async Task FetchControllerInfo()
        {
            await FetchControllerVersion();

            await FetchSerialCapabilities();

            await FetchControllerCapabilities();

            await FetchMemoryId();

            await FetchSucNodeId();
        }

        public async Task FetchSucNodeId()
        {
            var sucNodeIdFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.GetSucNodeId));
            SucNodeId = sucNodeIdFrame.First().Payload[0];
        }

        public async Task FetchMemoryId()
        {
            var memoryIdFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.MemoryGetId));

            var memoryIdBytes = memoryIdFrame.First().Payload;
            if (memoryIdBytes.Length != 5)
                return;

            HomeId = memoryIdBytes.Take(4).ToArray();

            Id = memoryIdBytes[4];
        }

        public async Task FetchControllerCapabilities()
        {
            var controllerCapsFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.GetControllerCapabilities));

            Capabilities = (ControllerCapabilities)controllerCapsFrame.First().Payload[0];
        }

        public async Task FetchSerialCapabilities()
        {
            var serialCapabilitiesFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.SerialGetCapabilities));

            var payload = serialCapabilitiesFrame.First().Payload;
            if (payload.Length <= 8)
                return;

            Version.ApplicationVersion = payload[0];
            Version.ApplicationSubVersion = payload[1];

            // todo: process capability bitmask
        }

        public async Task FetchControllerVersion()
        {
            var versionFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.GetVersion));

            var utf7 = new UTF7Encoding();
            var bytes = versionFrame.First().Payload;
            if (bytes.Length > 12)
            {
                Version.Version = utf7.GetString(bytes, 0, 12);
                if (Version.Version.EndsWith("\0"))
                {
                    Version.Version = Version.Version.Remove(Version.Version.Length - 1, 1);
                    var strArray = Version.Version.Replace("Z-Wave", "").Trim().Split('.');
                    if (strArray.Length == 2 && byte.TryParse(strArray[0], out byte protocolVersion) && byte.TryParse(strArray[1], out byte protocolSubVersion))
                    {
                        Version.ProtocolVersion = protocolVersion;
                        Version.ProtocolSubVersion = protocolSubVersion;
                    }
                }

                Version.Library = (Libraries)bytes[12];
            }
        }

        public async Task DiscoverNodes()
        {
            var serialFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.DiscoverNodes));

            _nodes = new List<ZWaveNode>();

            var frame = serialFrame.First();
            SerialApiVersion = frame.Payload[0];
            IsSlaveApi = (frame.Payload[1] & 1) != 0;
            var bitmaskStartIdx = 3;
            var bitmaskLength = frame.Payload[2];
            ChipType = frame.Payload[bitmaskStartIdx + bitmaskLength];
            ChipRevision = frame.Payload[bitmaskStartIdx + bitmaskLength + 1];
            var nodeIds = GetNodeIdsFromBitmask(frame.Payload.Skip(bitmaskStartIdx).Take(bitmaskLength).ToArray());

            foreach (byte nodeId in nodeIds)
            {
                _nodes.Add(new ZWaveNode { Id = nodeId });
            }
        }
        
        public async Task FetchNodeInfo()
        {
            foreach (var node in Nodes)
            {
                await FetchNodeInfo(node);
            }
        }

        public async Task FetchNodeInfo(ZWaveNode node)
        {
            var nodeInfoFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.GetNodeProtocolInfo, node.Id));

            var payload = nodeInfoFrame.First().Payload;
            node.ProtocolInfo.Capability = payload[0];
            node.ProtocolInfo.Security = payload[1];
            node.ProtocolInfo.Reserved = payload[2];
            node.ProtocolInfo.BasicType = _zwClasses.BasicDevices.SingleOrDefault(l => l.Key == payload[3]);
            node.ProtocolInfo.GenericType = _zwClasses.GenericDevices.SingleOrDefault(l => l.Key == payload[4]);
            node.ProtocolInfo.SpecificType = node.ProtocolInfo.GenericType?.SpecificDevices.SingleOrDefault(l => l.Key == payload[5]);

            // wait until node info was received before querying the next node as the
            // controller will cancel sends if another note info request is made.
            var nodeUpdatedEvent = new TaskCompletionSource<int>();

            FrameReceivedEventHandler appUpdateHandler = (sender, e) =>
            {
                if (e.Frame.Function == ZWaveFunction.ApplicationUpdate)
                {
                    nodeUpdatedEvent.SetResult(0);
                }
            };

            try
            {
                FrameReceived += appUpdateHandler;

                await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.RequestNodeInfo, node.Id));

                await nodeUpdatedEvent.Task;
            }
            finally
            {
                FrameReceived -= appUpdateHandler;
            }
        }

        public async Task<IReadOnlyCollection<ZWaveNode>> AddNodeNetworkWideInclusion(CancellationToken cancellationToken)
        {
            var addedNodes = new List<ZWaveNode>();
            do
            {
                try
                {
                    var addedNode = await AddNode(cancellationToken, ZWaveMode.NodeOptionNetworkWide | ZWaveMode.NodeOptionHighPower);
                    addedNodes.Add(addedNode);
                }
                catch(OperationCanceledException)
                {
                }
            } while (cancellationToken.IsCancellationRequested == false);

            return addedNodes.AsReadOnly();
        }

        public async Task<ZWaveNode> AddNode(CancellationToken cancellationToken, ZWaveMode mode = ZWaveMode.NodeAny)
        {
            var addNodeFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.AddNodeToNetwork, (byte)mode, SequenceNumber++));

            var nodeAddedEvent = new TaskCompletionSource<ZWaveNode>();
            using (cancellationToken.Register(nodeAddedEvent.SetCanceled))
            {
                FrameReceivedEventHandler nodeAddedHandler = (sender, e) =>
                {
                    if (e.Frame.Function == ZWaveFunction.AddNodeToNetwork)
                    {

                        var node = ExtractNodeFromFrame(e.Frame);
                        if (node != null)
                        {
                            _nodes.Add(node);

                            _logger.LogInformation(LogEvents.NodeInfoReceived, "Added Node: {znode} - Command Classes: {cmdClasses}", node, string.Join(", ", node.SupportedCommandClasses.Select(cc => cc.ToString())));

                            nodeAddedEvent.SetResult(node);
                        }
                    }
                };

                try
                {
                    FrameReceived += nodeAddedHandler;

                    var addedNode = await nodeAddedEvent.Task;

                    await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.AddNodeToNetwork, (byte)ZWaveMode.NodeStop, SequenceNumber++));

                    return addedNode;
                }
                finally
                {
                    FrameReceived -= nodeAddedHandler;
                }
            }                
        }

        public async Task<ZWaveNode> RemoveNode(CancellationToken cancellationToken)
        {
            var mode = ZWaveMode.NodeAny;
            await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.RemoveNodeFromNetwork, (byte)mode, SequenceNumber++));

            var nodeRemovedEvent = new TaskCompletionSource<ZWaveNode>();
            using (cancellationToken.Register(nodeRemovedEvent.SetCanceled))
            {
                FrameReceivedEventHandler nodeRemovedHandler = (sender, e) =>
                {
                    if (e.Frame.Function == ZWaveFunction.RemoveNodeFromNetwork)
                    {
                        var node = ExtractNodeFromFrame(e.Frame);

                        if (node != null)
                        {
                            _nodes.RemoveAll(n => n.Id == node.Id);

                            _logger.LogInformation(LogEvents.NodeInfoReceived, "Removed Node: {znode} - Command Classes: {cmdClasses}", node, string.Join(", ", node.SupportedCommandClasses.Select(cc => cc.ToString())));

                            nodeRemovedEvent.SetResult(node);
                        }
                    }
                };

                try
                {
                    FrameReceived += nodeRemovedHandler;

                    var addedNode = await nodeRemovedEvent.Task;

                    await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.RemoveNodeFromNetwork, (byte)ZWaveMode.NodeStop, SequenceNumber++));

                    return addedNode;
                }
                finally
                {
                    FrameReceived -= nodeRemovedHandler;
                }
            };
        }

        private ZWaveNode ExtractNodeFromFrame(Frame frame)
        {
            var payload = frame.Payload;
            var funcId = payload[0];

            if (payload.Length < 1)
            {
                return null;
            }

            var status = (NodeStatus)payload[1];

            if (status == NodeStatus.AddingRemovingSlave)
            {
                var nodeId = payload[2];
                var dataLength = payload[3];

                if (dataLength > 0)
                {
                    var basicKey = payload[4];
                    var genericKey = payload[5];
                    var specificKey = payload[6];
                    var cmdClassKeys = payload.Skip(7).Take(dataLength - 7);

                    var node = new ZWaveNode { Id = nodeId };

                    node.ProtocolInfo.BasicType = _zwClasses.BasicDevices.SingleOrDefault(l => l.Key == basicKey);
                    node.ProtocolInfo.GenericType = _zwClasses.GenericDevices.SingleOrDefault(l => l.Key == genericKey);
                    node.ProtocolInfo.SpecificType = node.ProtocolInfo.GenericType?.SpecificDevices.SingleOrDefault(l => l.Key == specificKey);

                    node.SupportedCommandClasses = cmdClassKeys.SelectMany(key => _zwClasses.CommandClasses.Where(c => c.Key == key)).ToList();

                    return node;
                }
            }

            return null;
        }

        private static List<byte> GetNodeIdsFromBitmask(byte[] nodeIdBytes)
        {
            var nodeIds = new List<byte>();
            byte nodeId = 1;
            for (int i = 0; i < nodeIdBytes.Length; i++)
            {
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((nodeIdBytes[i] & 1 << bit) > 0)
                    {
                        nodeIds.Add(nodeId);
                    }
                    nodeId++;
                }
            }

            return nodeIds;
        }
    }
}
