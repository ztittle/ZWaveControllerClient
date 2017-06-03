using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZWaveControllerClient.CommandClasses;
using ZWaveControllerClient.Xml;

namespace ZWaveControllerClient.SerialIO
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
        private ZWaveClassesXmlParser _zwaveXmlParser;
		private readonly Queue<TaskCompletionSource<Frame>> _sendTasks = new Queue<TaskCompletionSource<Frame>>();
        private byte[] _dataReceivedBuffer = { };
        private const int RetryCount = 2;

        private List<ZWaveNode> _nodes = new List<ZWaveNode>();

        public delegate void FrameReceivedEventHandler(object sender, FrameEventArgs e);
        public event FrameReceivedEventHandler FrameReceived;

        public IReadOnlyList<ZWaveNode> Nodes => _nodes;

        public byte ChipType { get; private set; }
        public byte ChipRevision { get; private set; }
        public byte SerialApiVersion { get; private set; }
        public bool IsSlaveApi { get; private set; }

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

        private object _locker = new object();

        public ZWaveSerialController(string portName, ILogger logger)
        {
            _portName = portName;
            _logger = logger;
            _zwaveXmlParser = new ZWaveClassesXmlParser();

            _serialPort = new SerialPortStream(_portName);

            _serialPort.DataReceived += _serialPort_DataReceived;
            _serialPort.ErrorReceived += _serialPort_ErrorReceived;

            _serialPort.BaudRate = 115200;
            _serialPort.Parity = Parity.None;
            _serialPort.StopBits = StopBits.One;
        }

        private void _serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
		{
			var frameDataLength = _serialPort.BytesToRead;
            if (frameDataLength > 0)
            {
                var frameData = new byte[frameDataLength];

                while (_serialPort.Read(frameData, 0, frameDataLength) <= 0) {}

                _logger.LogInformation($"{DateTime.Now} Recv bytes: {string.Concat(frameData.Select(b => b.ToString("x02")))}");
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

                _logger.LogInformation($"{DateTime.Now} Recv frame: {responseFrame}");

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

        private const int TimeoutMs = 3000;

        public Task<Frame> DispatchFrameAsync(Frame frame)
        {
            return DispatchFrameAsync(frame, new CancellationTokenSource(TimeoutMs).Token);
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

                    _logger.LogError("Resending frame.", KeyVal(nameof(requestFrame), requestFrame), KeyVal(nameof(_retriesLeft), _retriesLeft));
                    DispatchFrame(requestFrame);
                }
            }, frame, 1000, 1000);

            DispatchFrame(frame);
            
            return await tcs.Task;
        }

        public void DispatchFrame(Frame frame)
        {
            _logger.LogInformation("Dispatch frame", KeyVal(nameof(frame), frame), KeyVal(nameof(_retriesLeft), _retriesLeft));
            Dispatch(frame.GetData());
        }

        public void Dispatch(params byte[] bytes)
        {
            _logger.LogInformation($"{DateTime.Now} Send bytes: {string.Concat(bytes.Select(b => b.ToString("x02")))}");
            _serialPort.Write(bytes, 0, bytes.Length);
            _serialPort.Flush();
        }

        public Task<Frame> SendCommand(ZWaveNode node, CommandClass commandClass, Command command, TransmitOptions transmitOptions, params byte[] bytes)
        {
             return DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_SEND_DATA, 
                new byte[] { node.Id, (byte)(bytes.Length + 2), commandClass.Key, command.Key }
                .Concat(bytes)
                .Concat(new byte[] { (byte)(transmitOptions), SequenceNumber++ }).ToArray()));
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
                            _logger.LogError("Request was cancelled.", KeyVal(nameof(lastRequestFrame), lastRequestFrame), KeyVal(nameof(responseFrame), responseFrame));
                        }
                        else if (responseFrame.Header == FrameHeader.NotAcknowledged)
                        {
                            _logger.LogError("Request was not acknowledged.", KeyVal(nameof(lastRequestFrame), lastRequestFrame), KeyVal(nameof(responseFrame), responseFrame));
                        }
                    }
                }

                if (solicitedResponseFrame != null)
                {
                    _retransTimer?.Dispose();
                    _retransTimer = null;
                    _sendTasks.Dequeue();
                    _logger.LogInformation($"Set result for {lastRequestFrame}", KeyVal(nameof(lastRequestFrame), lastRequestFrame));
                    lastSendTask.SetResult(solicitedResponseFrame);
                }
            }
            catch (Exception e)
            {
                _logger.LogException(e);
            }
        }

        private KeyValuePair<string, object> KeyVal(string key, object value)
        {
            return new KeyValuePair<string, object>(key, value);
        }

        private void ProcessReceivedFrame(Frame responseFrame)
        {
            if (responseFrame.IsChecksumValid)
            {
                DispatchFrame(Ack);
            }
            else
            {
                _logger.LogError("Checksum failed.", KeyVal(nameof(responseFrame), responseFrame));
                DispatchFrame(Nack);
                return;
            }

            switch (responseFrame.Function)
            {
                case ZWaveFunction.FUNC_ID_ZW_APPLICATION_UPDATE:
                    HandleApplicationUpdate(responseFrame);
                    break;
            }

            FrameReceived?.Invoke(this, new FrameEventArgs { Frame = responseFrame });
        }

        private void HandleApplicationUpdate(Frame responseFrame)
        {
            var status = (ApplicationUpdateStatus)responseFrame.Payload[0];

            if (status == ApplicationUpdateStatus.NODE_INFO_REQ_FAILED)
            {
                _logger.LogError("failed to get node status", KeyVal(nameof(responseFrame), responseFrame), KeyVal(nameof(status), status));
                return;
            }

            var nodeId = responseFrame.Payload[1];
            var znode = _nodes.Single(n => n.Id == nodeId);
            znode.UpdateStatus = status;
            if (status == ApplicationUpdateStatus.NODE_INFO_RECEIVED)
            {
                var nifLength = responseFrame.Payload[2];
                if (nifLength > 0)
                {
                    var nodeInfo = responseFrame.Payload.Skip(3).Take(nifLength).ToArray();

                    znode.SupportedCommandClasses = nodeInfo.SelectMany(key => _zwClasses.CommandClasses.Where(c => c.Key == key)).ToList();
                    _logger.LogInformation($"Node: {znode} - Command Classes: {string.Join(", ", znode.SupportedCommandClasses.Select(cc => cc.ToString()))}");
                }
            }
        }

        private void _serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            _logger.LogError(e.ToString(), KeyVal(nameof(e.EventType), e.EventType));
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

        public async Task Initialize(Stream xmlCmdClassesStream)
        {
            _zwClasses = _zwaveXmlParser.Parse(xmlCmdClassesStream);

            await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_GET_VERSION));

            await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_GET_CONTROLLER_CAPABILITIES));

            await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_MEMORY_GET_ID));

            await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_GET_SUC_NODE_ID));

            var serialFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_SERIAL_API_GET_INIT_DATA));

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
                var nodeInfoFrame = await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_GET_NODE_PROTOCOL_INFO, nodeId));

                var node = _nodes.Single(l => l.Id == nodeId);
                node.ProtocolInfo.BasicType = _zwClasses.BasicDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[3]);
                node.ProtocolInfo.GenericType = _zwClasses.GenericDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[4]);
                node.ProtocolInfo.SpecificType = node.ProtocolInfo.GenericType?.SpecificDevices.SingleOrDefault(l => l.Key == nodeInfoFrame.Payload[5]);

                // wait until node info was received before querying the next node as the
                // controller will cancel sends if another note info request is made.
                var nodeUpdatedEvent = new TaskCompletionSource<int>();

                FrameReceivedEventHandler appUpdateHandler = (sender, e) =>
                {
                    if (e.Frame.Function == ZWaveFunction.FUNC_ID_ZW_APPLICATION_UPDATE)
                    {
                        nodeUpdatedEvent.SetResult(0);
                    }
                };

                try
                {
                    FrameReceived += appUpdateHandler;

                    await DispatchFrameAsync(new Frame(FrameType.Request, ZWaveFunction.FUNC_ID_ZW_REQUEST_NODE_INFO, nodeId));

                    await nodeUpdatedEvent.Task;
                }
                finally
                {
                    FrameReceived -= appUpdateHandler;
                }
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
