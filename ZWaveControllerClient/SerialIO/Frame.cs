using System;
using System.Collections.Generic;
using System.Linq;

namespace ZWaveControllerClient.SerialIO
{
    /// <summary>
    /// See https://www.itu.int/rec/T-REC-G.9959
    /// </summary>
    public class Frame
    {
        public Frame(FrameType type, FrameHeader header)
        {
            Type = type;
            Header = header;
            Payload = new byte[] { };
        }

        public Frame(FrameType type, ZWaveFunction function)
            : this(type, function, null)
        {
        }

        public Frame(FrameType type, ZWaveFunction function, params byte[] payload)
        {
            Timestamp = DateTime.Now;
            Header = FrameHeader.StartOfFrame;
            Type = type;
            Function = function;
            Payload = payload ?? new byte[] { };
            UpdateChecksum();
        }

        public Frame(byte[] frameData)
        {
            // todo: throw exception if invalid checksum ?
            Timestamp = DateTime.Now;
            Header = (FrameHeader)frameData[0];

            if (Header == FrameHeader.StartOfFrame)
            {
                var frameLength = frameData[1];
                var payloadLength = frameLength - 3;

                if (frameData.Length > 4)
                {
                    Type = (FrameType)frameData[2];
                    Function = (ZWaveFunction)frameData[3];
                }

                Payload = frameData
                    .Skip(4)
                    .Take(payloadLength)
                    .ToArray();

                Checksum = frameData[frameData.Length - 1];
            }
            else
            {
                Payload = new byte[] { };
            }
        }

        public FrameHeader Header { get; set; }
        public FrameType Type { get; set; }
        public byte Length {
            get
            {
                if (Header == FrameHeader.StartOfFrame)
                {
                    // includes function, payload, and checksum.
                    return (byte)(3 + Payload.Length);
                }

                return 1;
            }
        }
        public ZWaveFunction Function { get; set; }
        private byte[] _payload;
        public byte[] Payload
        {
            get => _payload;
            private set
            {
                _payload = value ?? throw new ArgumentNullException(nameof(Payload));
            }
        }
        public byte Checksum { get; private set; }
        public DateTime Timestamp { get; set; }

        public bool IsChecksumValid {
            get
            {
                var calculatedChecksum = CalculateChecksum();
                return (calculatedChecksum == Checksum);
            }
        }

        public void UpdateChecksum()
        {
            Checksum = CalculateChecksum();
        }

        public byte[] GetData()
        {
            var frameData = new List<byte>(Length + 2)
            {
                (byte)Header,
                Length,
                (byte)Type,
                (byte)Function
            };
            frameData.AddRange(Payload);
            frameData.Add(Checksum);

            return frameData.ToArray();
        }
        
        public byte CalculateChecksum()
        {
            byte checksum = 0xFF;
            checksum ^= (byte)Type;
            checksum ^= Length;
            checksum ^= (byte)Function;

            for (var i = 0; i < Payload.Length ; i++)
            {
                checksum ^= Payload[i];
            }

            return checksum;
        }

        public override string ToString()
        {
            return $"{Header} {Function}";
        }
    }
}
