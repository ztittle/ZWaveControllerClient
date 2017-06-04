﻿using Microsoft.Extensions.Logging;
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
		private readonly Queue<TaskCompletionSource<Frame>> _sendTasks = new Queue<TaskCompletionSource<Frame>>();
        private byte[] _dataReceivedBuffer = { };
        private const int RetryCount = 2;
        private const int RetryDurationMs = 1100;
        private const int RequestTimeout = 3000;

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

                _logger.LogInformation(LogEvents.Receive, "Recv bytes: {bytes}", string.Concat(frameData.Select(b => b.ToString("x02"))));
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

        public Task<Frame> DispatchFrameAsync(Frame frame)
        {
            return DispatchFrameAsync(frame, new CancellationTokenSource(RequestTimeout).Token);
        }

        public async Task<Frame> DispatchFrameAsync(Frame frame, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<Frame>(frame);

            _sendTasks.Enqueue(tcs);

            _retriesLeft = RetryCount;

            cancellationToken.Register(state =>
            {
                var tcsFromState = (TaskCompletionSource<Frame>)state;
                if (_sendTasks.Count > 0 && _sendTasks.Peek() == tcsFromState)
                {
                    _retransTimer?.Dispose();
                    _retransTimer = null;
                    _sendTasks.Dequeue();
                    tcsFromState.SetCanceled();
                }
            }, tcs);

            lock (_locker)
            {
                if (_sendTasks.Count > 1)
                {
                    _sendTasks.Peek().Task.Wait();
                }
            }

            _retransTimer = new Timer(state =>
            {
                if (_retriesLeft-- == 0)
                {
                    _retransTimer.Dispose();
                }
                else
                {
                    var requestFrame = (Frame)state;

                    _logger.LogWarning(LogEvents.RequestFrameRetry, "Resending frame {requestFrame}, retries left: {_retriesLeft}.", requestFrame, _retriesLeft);
                    DispatchFrame(requestFrame);
                }
            }, frame, RetryDurationMs, RetryDurationMs);

            DispatchFrame(frame);
            
            return await tcs.Task;
        }

        public void DispatchFrame(Frame frame)
        {
            _logger.LogInformation(LogEvents.DispatchFrame, "Dispatch frame {frame}, retries left: {_retriesLeft}.", frame, _retriesLeft);
            Dispatch(frame.GetData());
        }

        public void Dispatch(params byte[] bytes)
        {
            _logger.LogInformation(LogEvents.Dispatch, "Send bytes: {bytes}", string.Concat(bytes.Select(b => b.ToString("x02"))));
            _serialPort.Write(bytes, 0, bytes.Length);
            _serialPort.Flush();
        }

        public Task<Frame> SendCommand(byte nodeId, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes)
        {
            return DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.SendData,
               new byte[] { nodeId, (byte)(bytes.Length + 2), commandClass.Key, command.Key }
               .Concat(bytes)
               .Concat(new byte[] { (byte)(transmitOptions), SequenceNumber++ }).ToArray()));
        }

        public Task<Frame> SendCommand(ZWaveNode node, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes)
        {
            return SendCommand(node.Id, commandClass, command, transmitOptions, bytes);
        }

        private void ProcessFrames(IEnumerable<Frame> responseFrames)
        {
            try
            {
                Frame solicitedResponseFrame = null;
                TaskCompletionSource<Frame> lastSendTask = null;
                Frame lastRequestFrame = null;

                if (_sendTasks.Count > 0)
                {
                    lastSendTask = _sendTasks.Peek();
                    lastRequestFrame = (Frame)lastSendTask.Task.AsyncState;
                }

                foreach (var responseFrame in responseFrames)
                {
                    if (responseFrame.Header == FrameHeader.StartOfFrame)
                    {
                        ProcessReceivedFrame(responseFrame);

                        if (lastRequestFrame?.Function == responseFrame.Function &&
                            solicitedResponseFrame == null &&
                            responseFrame.Type == FrameType.Response &&
                            responseFrame.IsChecksumValid)
                        {
                            solicitedResponseFrame = responseFrame;
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

                if (solicitedResponseFrame != null)
                {
                    _retransTimer?.Dispose();
                    _retransTimer = null;
                    _sendTasks.Dequeue();
                    _logger.LogInformation(LogEvents.ResponseReceived, "Set result for {lastRequestFrame}", lastRequestFrame);
                    lastSendTask.SetResult(solicitedResponseFrame);
                }
            }
            catch (Exception e)
            {
                _logger.LogCritical(LogEvents.ProcessFramesException, e, e.Message);
            }
        }

        private void ProcessReceivedFrame(Frame responseFrame)
        {
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
            SucNodeId = sucNodeIdFrame.Payload[0];
        }

        public async Task FetchMemoryId()
        {
            var memoryIdFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.MemoryGetId));

            var memoryIdBytes = memoryIdFrame.Payload;
            if (memoryIdBytes.Length != 5)
                return;

            HomeId = memoryIdBytes.Take(4).ToArray();

            Id = memoryIdBytes[4];
        }

        public async Task FetchControllerCapabilities()
        {
            var controllerCapsFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.GetControllerCapabilities));

            Capabilities = (ControllerCapabilities)controllerCapsFrame.Payload[0];
        }

        public async Task FetchSerialCapabilities()
        {
            var serialCapabilitiesFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.SerialGetCapabilities));

            var payload = serialCapabilitiesFrame.Payload;
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
            var bytes = versionFrame.Payload;
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

            var frame = serialFrame;
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

            node.ProtocolInfo.BasicType = _zwClasses.BasicDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[3]);
            node.ProtocolInfo.GenericType = _zwClasses.GenericDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[4]);
            node.ProtocolInfo.SpecificType = node.ProtocolInfo.GenericType?.SpecificDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[5]);

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
                        nodeIds.Add(nodeId++);
                    }
                }
            }

            return nodeIds;
        }
    }
}
